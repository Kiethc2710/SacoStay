namespace SacoStayAPI.Model.DTOs
{
    public class UpdateOptionDTO
    {
        public int? OptionId { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
