using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class VoteRoomDTO
    {
        [Required(ErrorMessage = "Trạng thái vote không được để trống.")]
        [RegularExpression("Like|Dislike", ErrorMessage = "Vượt quá phạm vi dữ liệu: Chỉ chấp nhận 'Like' hoặc 'Dislike'.")]
        public string VoteStatus { get; set; } = "Like";
    }
}
