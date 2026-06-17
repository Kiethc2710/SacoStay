using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.DTOs
{
    public class FinalizeSpaceDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng trọ muốn chốt.")]
        public Guid ShortlistId { get; set; }
    }
}
