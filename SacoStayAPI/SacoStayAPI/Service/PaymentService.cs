using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

            decimal amount = packageName.ToUpper() switch
            {
                "BASIC" => 53000,
                "LITE" => 295000,
                "PRO" => 737500,
                "ELITE" => 1475000,
                _ => throw new ArgumentException("Gói không hợp lệ.")
            };

            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var transaction = new PaymentTransaction
            {
                OrderId = orderCode.ToString(),
                Amount = amount,
                Status = "Pending",
                PaymentMethod = "PayOS",
                RoomPostId = roomPostId,
                PackageName = packageName.ToUpper(),
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

            var description = $"Thanh toan {packageName.ToUpper()}";
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
                transaction.Status = "Success";
                if (transaction.RoomPostId.HasValue)
                {
                    var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(transaction.RoomPostId.Value);
                    if (roomPost != null)
                    {
                        roomPost.PackageTier = transaction.PackageName ?? "BASIC";
                        if (roomPost.PackageExpiresAt.HasValue && roomPost.PackageExpiresAt > DateTime.UtcNow)
                            roomPost.PackageExpiresAt = roomPost.PackageExpiresAt.Value.AddDays(30);
                        else
                            roomPost.PackageExpiresAt = DateTime.UtcNow.AddDays(30);

                        if (roomPost.Status == "PendingPayment")
                            roomPost.Status = "PendingApproval";

                        _unitOfWork.Repository<RoomPost>().Update(roomPost);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                transaction.Status = "Failed";
            }

            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.CompleteAsync();
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
                transaction.Status = "Success";
                if (transaction.RoomPostId.HasValue)
                {
                    var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(transaction.RoomPostId.Value);
                    if (roomPost != null)
                    {
                        roomPost.PackageTier = transaction.PackageName ?? "BASIC";
                        if (roomPost.PackageExpiresAt.HasValue && roomPost.PackageExpiresAt > DateTime.UtcNow)
                            roomPost.PackageExpiresAt = roomPost.PackageExpiresAt.Value.AddDays(30);
                        else
                            roomPost.PackageExpiresAt = DateTime.UtcNow.AddDays(30);
                        if (roomPost.Status == "PendingPayment")
                            roomPost.Status = "PendingApproval";
                        _unitOfWork.Repository<RoomPost>().Update(roomPost);
                    }
                }
            }

            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.CompleteAsync();
        }

        private static string CreateHmacSha256(string data, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
