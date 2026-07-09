using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public class PaymentService : IPaymentService
    {
        private static readonly HttpClient HttpClient = new();

        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly INotificationDispatcher _notificationDispatcher;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PaymentService> logger, INotificationDispatcher notificationDispatcher)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _notificationDispatcher = notificationDispatcher;
        }

        public async Task<string> CreatePackagePaymentUrlAsync(Guid roomPostId, string packageName)
        {
            var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(roomPostId);
            if (roomPost == null) throw new ArgumentException("Bài đăng không tồn tại.");

            var tier = ToLandlordTierCode(packageName);
            var amount = GetLandlordPackageAmount(tier);
            await CancelSupersededPendingLandlordPaymentsAsync(roomPostId);
            return await CreatePaymentLinkAsync(amount, tier, roomPostId, null, "Landlord");
        }

        public async Task<string> CreateTenantPackagePaymentUrlAsync(Guid userId, string packageName)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("Người dùng không tồn tại.");

            var amount = GetTenantPackageAmount(packageName);
            return await CreatePaymentLinkAsync(amount, $"TENANT_{packageName.ToUpper()}", null, userId, "Tenant");
        }

        private async Task<string> CreatePaymentLinkAsync(decimal amount, string packageCode, Guid? roomPostId, Guid? userId, string buyerType)
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var transaction = new PaymentTransaction
            {
                OrderId = orderCode.ToString(),
                Amount = amount,
                Status = "Pending",
                PaymentMethod = "PayOS",
                RoomPostId = roomPostId,
                UserId = userId,
                PackageName = packageCode,
                BuyerType = buyerType,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            var clientId = _configuration["PayOS:ClientId"] ?? string.Empty;
            var apiKey = _configuration["PayOS:ApiKey"] ?? string.Empty;
            var checksumKey = _configuration["PayOS:ChecksumKey"] ?? string.Empty;
            var baseUrl = _configuration["PayOS:BaseUrl"] ?? "https://api-merchant.payos.vn";
            var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? _configuration["Frontend:SecondaryBaseUrl"] ?? "https://sacostay.id.vn").TrimEnd('/');
            var apiBaseUrl = _configuration["PayOS:ApiBaseUrl"]?.TrimEnd('/') ?? frontendBaseUrl;

            // CancelUrl/ReturnUrl gọi BE trước, BE sẽ xử lý DB rồi redirect về FE
            var cancelUrl = $"{apiBaseUrl}/api/Payment/payos-return?source=cancel";
            var returnUrl = $"{apiBaseUrl}/api/Payment/payos-return?source=return";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(checksumKey))
                throw new ArgumentException("Thiếu cấu hình PayOS.");

            var description = packageCode;
            var signatureData = $"amount={amount:0}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
            var signature = CreateHmacSha256(signatureData, checksumKey);

            var payload = new
            {
                orderCode,
                amount = (int)amount,
                description,
                cancelUrl,
                returnUrl,
                signature
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v2/payment-requests");
            request.Headers.Add("x-client-id", clientId);
            request.Headers.Add("x-api-key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS create payment failed: {StatusCode} - {Content}", response.StatusCode, content);
                throw new ArgumentException("Không thể tạo link thanh toán PayOS.");
            }

            var payosResponse = JsonSerializer.Deserialize<PayOSCreateResponseDTO>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var checkoutUrl = payosResponse?.Data?.CheckoutUrl;
            if (string.IsNullOrWhiteSpace(checkoutUrl))
                throw new ArgumentException("PayOS không trả về checkoutUrl.");

            return checkoutUrl;
        }

        public async Task HandleReturnAsync(IQueryCollection query)
        {
            var orderCode = query["orderCode"].ToString();
            var status = query["status"].ToString();
            var cancel = query["cancel"].ToString();

            var transactions = await _unitOfWork.Repository<PaymentTransaction>().FindAsync(t => t.OrderId == orderCode);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null || transaction.Status == "Cancelled" || transaction.Status == "Success") return;

            var isCancelled = cancel.Equals("true", StringComparison.OrdinalIgnoreCase)
                          || status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)
                          || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase);

            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                await ApplySuccessAsync(transaction);
            }
            else if (isCancelled)
            {
                transaction.Status = "Cancelled";
                _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
                await _unitOfWork.CompleteAsync();
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                transaction.Status = "Failed";
                _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task<string> BuildFrontendReturnUrlAsync(IQueryCollection query)
        {
            var orderCode = query["orderCode"].ToString();
            var statusRaw = query["status"].ToString();
            var cancelRaw = query["cancel"].ToString();

            // PayOS sends cancel=true when user cancels payment
            var isCancelled = cancelRaw.Equals("true", StringComparison.OrdinalIgnoreCase)
                           || statusRaw.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)
                           || statusRaw.Equals("cancelled", StringComparison.OrdinalIgnoreCase);

            var payStatus = isCancelled
                ? "cancelled"
                : (statusRaw.Equals("PAID", StringComparison.OrdinalIgnoreCase) ||
                   statusRaw.Equals("success", StringComparison.OrdinalIgnoreCase))
                    ? "success"
                    : "failed";

            var transactions = await _unitOfWork.Repository<PaymentTransaction>().FindAsync(t => t.OrderId == orderCode);
            var transaction = transactions.FirstOrDefault();
            var context = string.Equals(transaction?.BuyerType, "Tenant", StringComparison.OrdinalIgnoreCase)
                ? "tenant"
                : "landlord";

            var baseUrl = (_configuration["Frontend:BaseUrl"] ?? _configuration["Frontend:SecondaryBaseUrl"] ?? "https://sacostay.id.vn").TrimEnd('/');
            return $"{baseUrl}/payment/result?status={payStatus}&context={context}&orderId={Uri.EscapeDataString(orderCode)}";
        }

        public async Task HandleWebhookAsync(string payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var data = root.GetProperty("data");
            var orderCode = data.GetProperty("orderCode").GetInt64().ToString();
            var code = data.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;

            var transactions = await _unitOfWork.Repository<PaymentTransaction>().FindAsync(t => t.OrderId == orderCode);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null || transaction.Status == "Cancelled") return;

            if (code == "00")
            {
                await ApplySuccessAsync(transaction);
            }
        }

        private async Task CancelSupersededPendingLandlordPaymentsAsync(Guid roomPostId)
        {
            var pending = (await _unitOfWork.Repository<PaymentTransaction>().FindAsync(
                t => t.RoomPostId == roomPostId && t.Status == "Pending" && t.BuyerType == "Landlord")).ToList();

            var changed = false;
            foreach (var t in pending)
            {
                t.Status = "Cancelled";
                _unitOfWork.Repository<PaymentTransaction>().Update(t);
                changed = true;
            }

            if (changed)
                await _unitOfWork.CompleteAsync();
        }

        private async Task ApplySuccessAsync(PaymentTransaction transaction)
        {
            if (transaction.Status == "Success") return;
            if (transaction.Status == "Cancelled") return;

            transaction.Status = "Success";

            if (transaction.BuyerType == "Landlord" && transaction.RoomPostId.HasValue)
            {
                var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(transaction.RoomPostId.Value);
                if (roomPost != null)
                {
                    roomPost.PackageTier = ToLandlordTierCode(transaction.PackageName);
                    if (roomPost.PackageExpiresAt.HasValue && roomPost.PackageExpiresAt > DateTime.UtcNow)
                        roomPost.PackageExpiresAt = roomPost.PackageExpiresAt.Value.AddDays(30);
                    else
                        roomPost.PackageExpiresAt = DateTime.UtcNow.AddDays(30);

                    roomPost.Status = "PendingApproval";
                    _unitOfWork.Repository<RoomPost>().Update(roomPost);

                    await _notificationDispatcher.NotifyAsync(
                        roomPost.UserId,
                        "Thanh toán gói thành công",
                        $"Bài đăng '{roomPost.Title}' đã thanh toán thành công và đang chờ admin duyệt.",
                        "payment",
                        $"/owner/my-posts?payment=success&roomPostId={roomPost.Id}");
                }
            }
            else if (transaction.BuyerType == "Tenant" && transaction.UserId.HasValue)
            {
                var account = await _unitOfWork.Repository<Account>().GetByIdAsync(transaction.UserId.Value);
                if (account != null)
                {
                    account.TenantPackageType = "Premium";
                    account.TenantPackageExpiresAt = account.TenantPackageExpiresAt.HasValue && account.TenantPackageExpiresAt > DateTime.UtcNow
                        ? account.TenantPackageExpiresAt.Value.AddDays(30)
                        : DateTime.UtcNow.AddDays(30);
                    _unitOfWork.Repository<Account>().Update(account);

                    await _notificationDispatcher.NotifyAsync(
                        account.Id,
                        "Nâng cấp Premium thành công",
                        "Tài khoản của bạn đã được nâng cấp Premium thành công.",
                        "payment",
                        "/membership?payment=success");
                }
            }

            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.CompleteAsync();
        }

        private static decimal GetLandlordPackageAmount(string packageName)
        {
            var tier = ToLandlordTierCode(packageName);
            return tier switch
            {
                "BASIC" => 53000,
                "LITE" => 295000,
                "PRO" => 737500,
                "ELITE" => 1475000,
                _ => throw new ArgumentException("Gói landlord không hợp lệ.")
            };
        }

        private static decimal GetTenantPackageAmount(string packageName) => packageName.ToUpper() switch
        {
            "PREMIUM" => 80000,
            _ => throw new ArgumentException("Gói tenant không hợp lệ.")
        };

        private async Task<IEnumerable<TransactionHistoryDTO>> GetTransactionHistoryInternalAsync(Guid userId)
        {
            var roomPosts = (await _unitOfWork.Repository<RoomPost>().FindAsync(r => r.UserId == userId)).ToList();
            var roomPostIds = roomPosts.Select(r => r.Id).ToList();

            var transactions = (await _unitOfWork.Repository<PaymentTransaction>()
                .FindAsync(t => t.UserId == userId || (t.RoomPostId.HasValue && roomPostIds.Contains(t.RoomPostId.Value))))
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var roomLookup = roomPosts.ToDictionary(r => r.Id, r => r.Title);

            return transactions.Select(t => new TransactionHistoryDTO
            {
                OrderId = t.OrderId,
                Amount = t.Amount,
                Status = t.Status,
                PaymentMethod = t.PaymentMethod,
                TransactionNo = t.TransactionNo,
                CreatedAt = t.CreatedAt,
                RoomPostId = t.RoomPostId,
                RoomTitle = t.RoomPostId.HasValue && roomLookup.TryGetValue(t.RoomPostId.Value, out var title) ? title : null,
                PackageName = t.PackageName
            }).ToList();
        }

        public Task<IEnumerable<TransactionHistoryDTO>> GetTransactionHistoryAsync(Guid userId)
            => GetTransactionHistoryInternalAsync(userId);

        /// <summary>BASIC | LITE | PRO | ELITE — không dùng tiền tố LANDLORD_.</summary>
        private static string ToLandlordTierCode(string? packageName)
        {
            var s = (packageName ?? "BASIC").Trim().ToUpperInvariant();
            if (s.StartsWith("LANDLORD_", StringComparison.Ordinal))
                s = s["LANDLORD_".Length..];
            return s is "BASIC" or "LITE" or "PRO" or "ELITE" ? s : "BASIC";
        }

        private static string CreateHmacSha256(string data, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
