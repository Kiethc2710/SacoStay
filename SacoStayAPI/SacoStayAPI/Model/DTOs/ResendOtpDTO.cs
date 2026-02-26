using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class ResendOtpDTO
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

    }
}
