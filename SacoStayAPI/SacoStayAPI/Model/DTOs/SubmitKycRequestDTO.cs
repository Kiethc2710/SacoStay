using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class SubmitKycRequestDTO
    {
        [Required(ErrorMessage = "Vui lòng tải lên ảnh mặt trước CCCD")]
        public IFormFile FrontIdImage { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên ảnh mặt sau CCCD")]
        public IFormFile BackIdImage { get; set; }

        [Required(ErrorMessage = "Vui lòng tải lên ảnh chụp khuôn mặt (Selfie)")]
        public IFormFile SelfieImage { get; set; }

        public IFormFile? VneidScreenshot { get; set; }
    }
}
