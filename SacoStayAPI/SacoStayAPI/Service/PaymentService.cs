using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        // Đổi tên hàm và nhận vào ID phòng + Tên gói
        public async Task<string> CreatePackagePaymentUrlAsync(Guid roomPostId, string packageName)
        {
            var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(roomPostId);
            if (roomPost == null) throw new ArgumentException("Bài đăng không tồn tại.");

            // Tính tiền theo gói
            decimal amount = packageName.ToUpper() switch
            {
                "BASIC" => 53000,
                "LITE" => 295000,
                "PRO" => 737500,
                "ELITE" => 1475000,
                _ => throw new ArgumentException("Gói không hợp lệ.")
            };

            var orderId = Guid.NewGuid().ToString();

            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = "Pending",
                PaymentMethod = "VNPay",
                RoomPostId = roomPostId,
                PackageName = packageName.ToUpper(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PaymentTransaction>().AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            // SỬA: Thêm toán tử chống null để xóa bỏ các Warning CS8604 khi tạo SortedDictionary
            var vnp_TmnCode = _configuration["VNPay:TmnCode"] ?? string.Empty;
            var vnp_HashSecret = _configuration["VNPay:HashSecret"] ?? string.Empty;
            var vnp_Url = _configuration["VNPay:BaseUrl"] ?? string.Empty;
            var vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"] ?? string.Empty;

            var vnp_Params = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnp_TmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", "127.0.0.1" },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"ThanhToan_{packageName.ToUpper()}_{roomPostId.ToString().Substring(0, 8)}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", vnp_ReturnUrl },
                { "vnp_TxnRef", orderId }
            };

            var hashData = new StringBuilder();
            var queryString = new StringBuilder();

            foreach (var kvp in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    // SỬA: Sử dụng Uri.EscapeDataString chuẩn RFC 3986 thay cho WebUtility.UrlEncode cũ
                    var encodedKey = Uri.EscapeDataString(kvp.Key);
                    var encodedValue = Uri.EscapeDataString(kvp.Value);

                    hashData.Append($"{encodedKey}={encodedValue}&");
                    queryString.Append($"{encodedKey}={encodedValue}&");
                }
            }

            if (hashData.Length > 0)
            {
                hashData.Length -= 1;
                queryString.Length -= 1;
            }

            var secretBytes = Encoding.UTF8.GetBytes(vnp_HashSecret);
            var dataBytes = Encoding.UTF8.GetBytes(hashData.ToString());

            using var hmac = new HMACSHA512(secretBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);

            // SỬA: Đổi sang .ToUpper() để khớp chuẩn so khớp mã hash của hệ thống VNPay
            var secureHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();

            queryString.Append($"&vnp_SecureHash={secureHash}");

            return $"{vnp_Url}?{queryString}";
        }

        public async Task HandleReturnAsync(IQueryCollection query)
        {
            var orderId = query["vnp_TxnRef"].ToString();
            var responseCode = query["vnp_ResponseCode"].ToString();

            var transactions = await _unitOfWork.Repository<PaymentTransaction>().FindAsync(t => t.OrderId == orderId);
            var transaction = transactions.FirstOrDefault();

            if (transaction == null) return;

            if (responseCode == "00")
            {
                transaction.Status = "Success";
                transaction.TransactionNo = query["vnp_TransactionNo"];

                if (transaction.RoomPostId.HasValue)
                {
                    var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(transaction.RoomPostId.Value);
                    if (roomPost != null)
                    {
                        // 1. Cập nhật tên gói dịch vụ mới
                        roomPost.PackageTier = transaction.PackageName ?? "BASIC";

                        // 2. Logic cộng dồn ngày: Nếu bài đăng vẫn còn hạn thì cộng dồn thêm 30 ngày, nếu đã hết hạn thì tính 30 ngày từ hôm nay
                        if (roomPost.PackageExpiresAt.HasValue && roomPost.PackageExpiresAt > DateTime.UtcNow)
                        {
                            roomPost.PackageExpiresAt = roomPost.PackageExpiresAt.Value.AddDays(30);
                        }
                        else
                        {
                            roomPost.PackageExpiresAt = DateTime.UtcNow.AddDays(30);
                        }

                        // 3. Xử lý trạng thái:
                        // - Nếu là bài mới toanh (PendingPayment) -> Chuyển sang chờ Admin duyệt
                        // - Nếu là bài cũ đang hoạt động (Active) -> Mua gói xong vẫn giữ nguyên Active cho lên sóng luôn
                        if (roomPost.Status == "PendingPayment")
                        {
                            roomPost.Status = "PendingApproval";
                        }

                        _unitOfWork.Repository<RoomPost>().Update(roomPost);
                    }
                }
            }
            else
            {
                transaction.Status = "Failed";
            }

            _unitOfWork.Repository<PaymentTransaction>().Update(transaction);
            await _unitOfWork.CompleteAsync();
        }
    }
}