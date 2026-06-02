using SacoStayAPI.Model.DTOs;

namespace SacoStayAPI.Service
{
    public interface IUserProfileService
    {
        Task<List<string>> UploadProfileImagesAsync(Guid userId, List<IFormFile> files);
        Task<bool> DeleteProfileImageAsync(Guid userId, string imageUrl);
        Task<IEnumerable<ProfileImageDTO>> GetProfileImagesAsync(Guid userId);
    }
}
