using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public class PhotoService : IPhotoService
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _s3Client;

        public PhotoService(IConfiguration config, IAmazonS3 s3Client)
        {
            _config = config;
            _s3Client = s3Client;
        }

        /// <summary>
        /// Upload ảnh lên S3 và trả về URL truy cập public
        /// </summary>
        /// <param name="file">File ảnh từ Request</param>
        /// <param name="folderName">Tên folder trên S3 (ví dụ: "rooms", "users")</param>
        public async Task<string> UploadPhotoAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ hoặc trống.");

            var bucketName = _config["AWS:BucketName"];
            var region = _config["AWS:Region"];

            // Tạo tên file duy nhất bằng Guid để tránh trùng tên trên S3
            var fileName = $"{folderName}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            using var newStream = new MemoryStream();
            await file.CopyToAsync(newStream);
            newStream.Position = 0;

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = newStream,
                Key = fileName,
                BucketName = bucketName,
                CannedACL = S3CannedACL.PublicRead // Cấp quyền đọc công khai để hiển thị trên Web/App
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            // Trả về URL public của ảnh sau khi upload thành công
            return $"https://{bucketName}.s3.{region}.amazonaws.com/{fileName}";
        }

        /// <summary>
        /// Xóa ảnh trên S3 bằng URL của ảnh đó
        /// </summary>
        /// <param name="fileUrl">Đường dẫn URL toàn vẹn lưu trong DB</param>
        public async Task<bool> DeletePhotoAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return false;

            try
            {
                var bucketName = _config["AWS:BucketName"];

                // Bóc tách "Key" (đường dẫn file trên S3) từ URL
                // Ví dụ từ URL: https://sacostay.s3.amazonaws.com/rooms/abc-123.jpg -> lấy ra: "rooms/abc-123.jpg"
                var uri = new Uri(fileUrl);
                var key = uri.AbsolutePath.TrimStart('/');

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);

                return response.HttpStatusCode == HttpStatusCode.NoContent ||
                       response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                // Có thể bổ sung log lỗi ở đây nếu cần thiết
                return false;
            }
        }
    }
}