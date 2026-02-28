using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace SacoStayAPI.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        private readonly IConfiguration _configuration;

        public PaymentService(IPaymentRepository repo, IConfiguration configuration)
        {
            _repo = repo;
            _configuration = configuration;
        }

        //public async Task<string> CreatePayment(decimal amount)
        //{
        //    var orderId = Guid.NewGuid().ToString();

        //    var transaction = new PaymentTransaction
        //    {
        //        OrderId = orderId,
        //        Amount = amount,
        //        Status = "Pending",
        //        PaymentMethod = "VNPay"
        //    };

        //    await _repo.AddAsync(transaction);

        //    // TODO: build VNPay URL ở đây

        //    return "URL_THANH_TOAN";
        //}

        //    public async Task<string> CreatePayment(decimal amount)
        //    {
        //        var orderId = Guid.NewGuid().ToString();

        //        var transaction = new PaymentTransaction
        //        {
        //            OrderId = orderId,
        //            Amount = amount,
        //            Status = "Pending",
        //            PaymentMethod = "VNPay",
        //            CreatedAt = DateTime.Now
        //        };

        //        await _repo.AddAsync(transaction);

        //        // Nếu repo không tự SaveChanges thì thêm:
        //        // await _context.SaveChangesAsync();

        //        var vnp_TmnCode = _configuration["VNPay:TmnCode"];
        //        var vnp_HashSecret = _configuration["VNPay:HashSecret"];
        //        var vnp_Url = _configuration["VNPay:BaseUrl"];
        //        var vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"];

        //        // Sắp xếp param đúng chuẩn
        //        var vnp_Params = new SortedDictionary<string, string>
        //{
        //    { "vnp_Version", "2.1.0" },
        //    { "vnp_Command", "pay" },
        //    { "vnp_TmnCode", vnp_TmnCode },
        //    { "vnp_Amount", ((long)(amount * 100)).ToString() }, // *100
        //    { "vnp_CurrCode", "VND" },
        //    { "vnp_TxnRef", orderId },
        //    { "vnp_OrderInfo", "ThanhToan" },
        //    { "vnp_Locale", "vn" },
        //    { "vnp_ReturnUrl", vnp_ReturnUrl },
        //    { "vnp_IpAddr", "127.0.0.1" },
        //    { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
        //    { "vnp_OrderType", "other" }
        //};

        //        // 1) Tạo raw data để hash (chưa encode)
        //        var rawData = new StringBuilder();
        //        foreach (var kvp in vnp_Params)
        //        {
        //            if (!string.IsNullOrEmpty(kvp.Value))
        //            {
        //                rawData.Append($"{kvp.Key}={kvp.Value}&");
        //            }
        //        }
        //        // bỏ ký tự & cuối cùng
        //        rawData.Length -= 1;

        //        // 2) Ký hash SHA512 (chính xác VNPay)
        //        var secretBytes = Encoding.UTF8.GetBytes(vnp_HashSecret);
        //        var dataBytes = Encoding.UTF8.GetBytes(rawData.ToString());

        //        using var hmac = new HMACSHA512(secretBytes);
        //        var hashBytes = hmac.ComputeHash(dataBytes);
        //        var secureHash = BitConverter.ToString(hashBytes)
        //                            .Replace("-", "")
        //                            .ToLower();

        //        // 3) Build query string (encode)
        //        var queryString = new StringBuilder();
        //        foreach (var kvp in vnp_Params)
        //        {
        //            if (!string.IsNullOrEmpty(kvp.Value))
        //            {
        //                queryString.Append($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}&");
        //            }
        //        }


        //        // 4) Thêm secure hash encode
        //        queryString.Append($"vnp_SecureHash={secureHash}");

        //        // 5) Hoàn tất URL
        //        var paymentUrl = $"{vnp_Url}?{queryString.ToString()}";
        //        Console.WriteLine("RAW DATA:");
        //        Console.WriteLine(rawData.ToString());

        //        Console.WriteLine("SECURE HASH:");
        //        Console.WriteLine(secureHash);

        //        Console.WriteLine("FULL URL:");
        //        Console.WriteLine(paymentUrl);
        //        return paymentUrl;
        //    }
        public async Task<string> CreatePayment(decimal amount)
        {
            var orderId = Guid.NewGuid().ToString();

            var transaction = new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = "Pending",
                PaymentMethod = "VNPay",
                CreatedAt = DateTime.Now
            };

            await _repo.AddAsync(transaction);

            var vnp_TmnCode = _configuration["VNPay:TmnCode"];
            var vnp_HashSecret = _configuration["VNPay:HashSecret"];
            var vnp_Url = _configuration["VNPay:BaseUrl"];
            var vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"];

            // Tạo danh sách param và sort tự động
            var vnp_Params = new SortedDictionary<string, string>
    {
        { "vnp_Version", "2.1.0" },
        { "vnp_Command", "pay" },
        { "vnp_TmnCode", vnp_TmnCode },
        { "vnp_Amount", ((long)(amount * 100)).ToString() },
        { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
        { "vnp_CurrCode", "VND" },
        { "vnp_IpAddr", "127.0.0.1" },
        { "vnp_Locale", "vn" },
        { "vnp_OrderInfo", "ThanhToan" }, // không dấu, không space
        { "vnp_OrderType", "other" },
        { "vnp_ReturnUrl", vnp_ReturnUrl },
        { "vnp_TxnRef", orderId }
    };

            // =========================
            // 1. TẠO RAW DATA ĐỂ HASH (KHÔNG ENCODE)
            // =========================
            var rawData = new StringBuilder();

            foreach (var kvp in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    rawData.Append($"{kvp.Key}={kvp.Value}&");
                }
            }

            rawData.Length -= 1; // bỏ dấu & cuối

            // =========================
            // 2. TẠO CHỮ KÝ HMAC SHA512
            // =========================
            var secretBytes = Encoding.UTF8.GetBytes(vnp_HashSecret);
            var dataBytes = Encoding.UTF8.GetBytes(rawData.ToString());

            using var hmac = new HMACSHA512(secretBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);

            var secureHash = BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .ToLower();

            // =========================
            // 3. BUILD QUERY STRING (ENCODE)
            // =========================
            var queryString = new StringBuilder();

            foreach (var kvp in vnp_Params)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    queryString.Append($"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}&");
                }
            }

            // Thêm loại hash
            queryString.Append($"vnp_SecureHashType=HmacSHA512&");
            queryString.Append($"vnp_SecureHash={secureHash}");

            // =========================
            // 4. TẠO PAYMENT URL
            // =========================
            var paymentUrl = $"{vnp_Url}?{queryString}";

            return paymentUrl;
        }
        public async Task HandleReturnAsync(IQueryCollection query)
        {
            var orderId = query["vnp_TxnRef"];
            var responseCode = query["vnp_ResponseCode"];

            var transaction = await _repo.GetByOrderIdAsync(orderId);

            if (transaction == null)
                return;

            if (responseCode == "00")
            {
                transaction.Status = "Success";
                transaction.TransactionNo = query["vnp_TransactionNo"];
            }
            else
            {
                transaction.Status = "Failed";
            }

            await _repo.UpdateAsync(transaction);
        }
    }
}
