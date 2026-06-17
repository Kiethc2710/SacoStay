using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class CreateSpaceDTO
    {
        [Required(ErrorMessage = "Vui lòng cung cấp mã ID của đối tác (TargetUserId) để khởi tạo không gian chung.")]
        public Guid TargetUserId { get; set; }
    }
}
