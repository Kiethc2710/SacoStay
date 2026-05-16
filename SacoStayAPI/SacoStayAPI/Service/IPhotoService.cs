namespace SacoStayAPI.Service
{
    public interface IPhotoService
    {
        Task<string> UploadPhotoAsync(IFormFile file, string folderName);
        Task<bool> DeletePhotoAsync(string fileUrl);
    }
}
