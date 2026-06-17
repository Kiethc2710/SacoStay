using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class AddToShortlistDTO
    {
        [Required(ErrorMessage = "Vui lòng cung cấp mã phòng trọ.")]
        public Guid RoomId { get; set; }
    }
}
