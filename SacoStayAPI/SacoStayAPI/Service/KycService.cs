using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using SacoStayAPI.Service;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SacoStayAPI.Services
{
    public class KycService : IKycService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService _photoService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public KycService(
            IUnitOfWork unitOfWork,
            IPhotoService photoService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<(bool IsSuccess, string Message)> SubmitKycAsync(Guid userId, SubmitKycRequestDTO dto)
        {
            // 1. Kiểm tra trạng thái hồ sơ cũ trong hệ thống
            var existingRequest = await _unitOfWork.Repository<KycRequest>()
                .GetQueryable()
                .Where(k => k.UserId == userId && (k.Status == KycStatus.Pending || k.Status == KycStatus.Approved))
                .FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                if (existingRequest.Status == KycStatus.Approved)
                    return (false, "Tài khoản của bạn đã được xác minh danh tính rồi.");

                return (false, "Bạn đã nộp hồ sơ trước đó, hệ thống đang xử lý.");
            }

            // 2. Gọi FPT.AI để OCR mặt trước CCCD (Bóc tách thông tin tự động)
            var ocrData = await CallFptOcrAsync(dto.FrontIdImage);
            if (ocrData == null || string.IsNullOrEmpty(ocrData.Id))
            {
                return (false, "Không thể đọc được thông tin CCCD. Vui lòng chụp rõ nét mặt trước, không lóa sáng.");
            }

            // 3 & 4. Gọi FPT.AI Liveness V3 để KIỂM TRA NGƯỜI THẬT + SO KHỚP KHUÔN MẶT đồng thời
            double matchResult = await CallFptLivenessAsync(dto.SelfieVideo, dto.FrontIdImage);

            if (matchResult < 0)
            {
                return (false, "Phát hiện video selfie không hợp lệ hoặc không trùng khớp khuôn mặt. Vui lòng quay hình trực tiếp.");
            }

            // Ngưỡng tin cậy chuẩn của eKYC thường là >= 80% trùng khớp
            bool isFaceMatched = matchResult >= 80.0;

            // 5. Upload lưu trữ các tệp ảnh lên Cloud của bạn thông qua IPhotoService
            var frontUrl = await _photoService.UploadPhotoAsync(dto.FrontIdImage, "kyc-documents");
            var backUrl = await _photoService.UploadPhotoAsync(dto.BackIdImage, "kyc-documents");
            var selfieUrl = await _photoService.UploadPhotoAsync(dto.SelfieVideo, "kyc-documents");
            string? vneidUrl = dto.VneidScreenshot != null
                ? await _photoService.UploadPhotoAsync(dto.VneidScreenshot, "kyc-documents")
                : null;

            // 6. Đóng gói dữ liệu Entity
            var kycRequest = new KycRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FullName = ocrData.Name,       // Tự động điền họ tên từ AI
                NationalId = ocrData.Id,       // Tự động điền số CCCD từ AI
                FrontIdImageUrl = frontUrl,
                BackIdImageUrl = backUrl,
                SelfieImageUrl = selfieUrl,
                VneidScreenshotUrl = vneidUrl,
                CreatedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow,
                ReviewedBy = "FPT.AI Automated System"
            };

            // 7. Tự động đưa ra quyết định duyệt dựa trên kết quả AI
            if (isFaceMatched)
            {
                kycRequest.Status = KycStatus.Approved;
                kycRequest.AdminNote = $"Tự động duyệt hoàn toàn bằng hệ thống AI (Độ khớp mặt: {matchResult:F2}%)";

                // Tìm và kích hoạt cờ xác minh cho tài khoản người dùng
                var userAccount = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
                if (userAccount != null)
                {
                    userAccount.IsVerified = true;
                    _unitOfWork.Repository<Account>().Update(userAccount);
                }
            }
            else
            {
                kycRequest.Status = KycStatus.NeedReupload;
                kycRequest.AdminNote = $"Hệ thống từ chối: Khuôn mặt chụp selfie không trùng khớp với ảnh trên CCCD gốc (Độ khớp: {matchResult:F2}%).";
            }

            // 8. Đẩy xuống Database bằng Generic Repository của bạn
            // LƯU Ý: Đổi tên hàm ".Add" bên dưới thành ".AddAsync" hoặc ".Insert" cho đúng với IGenericRepository của bạn nếu bị báo lỗi đỏ.
            // SỬA LẠI THÀNH THẾ NÀY:
            await _unitOfWork.Repository<KycRequest>().AddAsync(kycRequest);

            if (await _unitOfWork.CompleteAsync() > 0)
            {
                if (kycRequest.Status == KycStatus.Approved)
                    return (true, "Xác minh danh tính (eKYC) hoàn tất thành công tự động!");

                return (false, $"Xác minh thất bại. {kycRequest.AdminNote}");
            }

            return (false, "Gặp lỗi hệ thống nội bộ khi lưu trữ hồ sơ xác minh.");
        }

        public async Task<object?> GetUserKycStatusAsync(Guid userId)
        {
            return await _unitOfWork.Repository<KycRequest>()
                .GetQueryable()
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new {
                    status = k.Status.ToString(),
                    adminNote = k.AdminNote,
                    submittedAt = k.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        // ====================================================================
        // CÁC HÀM GIAO TIẾP HTTP CLIENT VỚI API FPT.AI
        // ====================================================================

        private async Task<FptOcrResult?> CallFptOcrAsync(IFormFile file)
        {
            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["FptAiConfig:ApiKey"];
            var url = _configuration["FptAiConfig:OcrUrl"];

            client.DefaultRequestHeaders.Add("api-key", apiKey);

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "image", file.FileName);

            var response = await client.PostAsync(url, content);

            // 🌟 ĐƯA ĐOẠN ĐỌC LOG NÀY LÊN TRÊN ĐỂ BẮT ĐƯỢC NỘI DUNG LỖI 404 🌟
            var jsonString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\n==================================================");
            Console.WriteLine($">>> PHẢN HỒI THỰC TẾ TỪ FPT.AI (Mã trạng thái {response.StatusCode}):");
            Console.WriteLine(jsonString);
            Console.WriteLine("==================================================\n");

            // Sau khi in log xong mới check status để thoát hàm
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
            {
                var firstData = dataArray[0];
                return new FptOcrResult
                {
                    Id = firstData.GetProperty("id").GetString() ?? "",
                    Name = firstData.GetProperty("name").GetString() ?? ""
                };
            }
            return null;
        }

        //private async Task<bool> CallFptLivenessAsync(IFormFile selfieFile)
        //{
        //    try
        //    {
        //        var client = _httpClientFactory.CreateClient();
        //        var apiKey = _configuration["FptAiConfig:ApiKey"];
        //        var url = _configuration["FptAiConfig:LivenessUrl"];

        //        client.DefaultRequestHeaders.Add("api-key", apiKey);

        //        using var content = new MultipartFormDataContent();
        //        using var stream = selfieFile.OpenReadStream();
        //        var streamContent = new StreamContent(stream);
        //        streamContent.Headers.ContentType = new MediaTypeHeaderValue(selfieFile.ContentType);
        //        content.Add(streamContent, "image", selfieFile.FileName);

        //        var response = await client.PostAsync(url, content);
        //        if (!response.IsSuccessStatusCode) return false;

        //        var jsonString = await response.Content.ReadAsStringAsync();
        //        using var doc = JsonDocument.Parse(jsonString);
        //        var root = doc.RootElement;

        //        if (root.TryGetProperty("data", out var dataObj) && dataObj.TryGetProperty("is_live", out var isLiveProp))
        //        {
        //            // Xử lý an toàn cho cả dạng Boolean gốc hoặc chuỗi ký tự "True"/"False" từ FPT trả về
        //            if (isLiveProp.ValueKind == JsonValueKind.True) return true;
        //            if (isLiveProp.ValueKind == JsonValueKind.False) return false;
        //            if (isLiveProp.ValueKind == JsonValueKind.String)
        //            {
        //                return bool.TryParse(isLiveProp.GetString(), out bool parsedResult) && parsedResult;
        //            }
        //        }
        //        return false;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        private async Task<double> CallFptLivenessAsync(IFormFile videoFile, IFormFile frontIdImage)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiKey = _configuration["FptAiConfig:ApiKey"];
                var url = _configuration["FptAiConfig:LivenessUrl"]; // Link v3: https://api.fpt.ai/dmp/liveness/v3

                client.DefaultRequestHeaders.Add("api-key", apiKey);

                using var content = new MultipartFormDataContent();

                // 1. Đóng gói tệp tin Video quay từ webcam (Key bắt buộc của V3 là "video")
                using var videoStream = videoFile.OpenReadStream();
                var videoContent = new StreamContent(videoStream);
                videoContent.Headers.ContentType = new MediaTypeHeaderValue(videoFile.ContentType);
                content.Add(videoContent, "video", videoFile.FileName);

                // 2. Đóng gói luôn ảnh mặt trước CCCD để nhờ FPT.AI đối chiếu FaceMatch luôn
                using var imageStream = frontIdImage.OpenReadStream();
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(frontIdImage.ContentType);
                content.Add(imageContent, "cmnd", frontIdImage.FileName);

                var response = await client.PostAsync(url, content);
                var jsonString = await response.Content.ReadAsStringAsync();

                Console.WriteLine("\n==================================================");
                Console.WriteLine($">>> PHẢN HỒI LIVENESS V3 TỪ FPT.AI (Mã {response.StatusCode}):");
                Console.WriteLine(jsonString);
                Console.WriteLine("==================================================\n");

                if (!response.IsSuccessStatusCode) return -1;

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                bool isLive = false;
                double similarity = -1;

                // 3. Bóc tách kết quả Liveness (Đã sửa: Không phân biệt hoa thường)
                if (root.TryGetProperty("liveness", out var livenessObj) &&
                    livenessObj.TryGetProperty("is_live", out var isLiveProp))
                {
                    var isLiveStr = isLiveProp.GetString() ?? "";
                    isLive = isLiveStr.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                // Nếu phát hiện video giả lập hoặc ảnh chụp lại -> Trả về -1 coi như thất bại
                if (!isLive) return -1;

                // 4. Bóc tách kết quả FaceMatch tự động tích hợp bên trong gói V3
                if (root.TryGetProperty("face_match", out var faceMatchObj) &&
                    faceMatchObj.TryGetProperty("similarity", out var simProp))
                {
                    if (simProp.ValueKind == JsonValueKind.Number)
                    {
                        similarity = simProp.GetDouble();
                    }
                    else if (simProp.ValueKind == JsonValueKind.String)
                    {
                        var simStr = simProp.GetString() ?? "0";
                        // Dùng InvariantCulture để ép hệ thống luôn đọc dấu chấm (.) là số thập phân
                        double.TryParse(simStr, System.Globalization.CultureInfo.InvariantCulture, out similarity);
                    }
                }

                return similarity;
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> LỖI HỆ THỐNG LIVENESS V3: {ex.Message}");
                return -1;
            }
        }

        private async Task<double> CallFptFaceMatchAsync(IFormFile file1, IFormFile file2)
        {
            var client = _httpClientFactory.CreateClient();
            var apiKey = _configuration["FptAiConfig:ApiKey"];
            var url = _configuration["FptAiConfig:FaceMatchUrl"];

            client.DefaultRequestHeaders.Add("api-key", apiKey);

            using var content = new MultipartFormDataContent();

            using var stream1 = file1.OpenReadStream();
            var content1 = new StreamContent(stream1);
            content1.Headers.ContentType = new MediaTypeHeaderValue(file1.ContentType);
            content.Add(content1, "file[]", file1.FileName);

            using var stream2 = file2.OpenReadStream();
            var content2 = new StreamContent(stream2);
            content2.Headers.ContentType = new MediaTypeHeaderValue(file2.ContentType);
            content.Add(content2, "file[]", file2.FileName);

            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return -1;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataObj) && dataObj.TryGetProperty("similarity", out var simProp))
            {
                return simProp.GetDouble();
            }
            return -1;
        }

        private class FptOcrResult
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
    }
}