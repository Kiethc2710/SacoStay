namespace SacoStayAPI.Model.DTOs
{
    public class UploadProfileImageDTO
    {
        public List<IFormFile> Files { get; set; } = new();
    }
}
