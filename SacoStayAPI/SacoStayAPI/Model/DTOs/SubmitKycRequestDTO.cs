using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class SubmitKycRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng tải lên ảnh mặt trước CCCD")]
        public IFormFile FrontIdImage { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên ảnh mặt sau CCCD")]
        public IFormFile BackIdImage { get; set; }

        // Thay vì nhận ảnh, bây giờ sẽ nhận file video quay trực tiếp
        [Required(ErrorMessage = "Vui lòng tải lên video quay khuôn mặt")]
        public IFormFile SelfieVideo { get; set; }

        public IFormFile? VneidScreenshot { get; set; }
    }
}
