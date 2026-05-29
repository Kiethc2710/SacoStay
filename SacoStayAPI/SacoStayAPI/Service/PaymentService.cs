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

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreatePackagePaymentUrlAsync(Guid roomPostId, string packageName)
        {
            var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(roomPostId);
            if (roomPost == null) throw new ArgumentException("Bài đăng không tồn tại.");

            var amount = GetLandlordPackageAmount(packageName);
            return await CreatePaymentLinkAsync(amount, $"LANDLORD_{packageName.ToUpper()}", roomPostId, null, "Landlord");
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
            var cancelUrl = _configuration["PayOS:CancelUrl"] ?? string.Empty;
            var returnUrl = _configuration["PayOS:ReturnUrl"] ?? string.Empty;

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

            var transactions = await _unitOfWork.Repository<PaymentTransaction>().FindAsync(t => t.OrderId == orderCode);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null) return;

            if (status.Equals("PAID", StringComparison.OrdinalIgnoreCase) || status.Equals("success", StringComparison.OrdinalIgnoreCase))
            {
                await ApplySuccessAsync(transaction);
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                transaction.Status = "Failed";
                _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
                await _unitOfWork.CompleteAsync();
            }
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
            if (transaction == null) return;

            if (code == "00")
            {
                await ApplySuccessAsync(transaction);
            }
        }

        private async Task ApplySuccessAsync(PaymentTransaction transaction)
        {
            transaction.Status = "Success";

            if (transaction.BuyerType == "Landlord" && transaction.RoomPostId.HasValue)
            {
                var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(transaction.RoomPostId.Value);
                if (roomPost != null)
                {
                    roomPost.PackageTier = transaction.PackageName ?? "LANDLORD_BASIC";
                    if (roomPost.PackageExpiresAt.HasValue && roomPost.PackageExpiresAt > DateTime.UtcNow)
                        roomPost.PackageExpiresAt = roomPost.PackageExpiresAt.Value.AddDays(30);
                    else
                        roomPost.PackageExpiresAt = DateTime.UtcNow.AddDays(30);

                    roomPost.Status = "PendingApproval";
                    _unitOfWork.Repository<RoomPost>().Update(roomPost);
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
                }
            }

            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.CompleteAsync();
        }

        private static decimal GetLandlordPackageAmount(string packageName) => packageName.ToUpper() switch
        {
            "BASIC" => 53000,
            "LITE" => 295000,
            "PRO" => 737500,
            "ELITE" => 1475000,
            _ => throw new ArgumentException("Gói landlord không hợp lệ.")
        };

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

        private static string CreateHmacSha256(string data, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
