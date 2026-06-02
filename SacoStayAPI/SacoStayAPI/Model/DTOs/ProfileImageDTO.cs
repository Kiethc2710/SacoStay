namespace SacoStayAPI.Model.DTOs
{
    public class ProfileImageDTO
    {
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
