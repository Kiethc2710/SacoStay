using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService _photoService;

        public UserProfileService(IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
        }

        public async Task<List<string>> UploadProfileImagesAsync(Guid userId, List<IFormFile> files)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("Người dùng không tồn tại.");

            if (files == null || files.Count == 0)
                throw new ArgumentException("Vui lòng chọn ít nhất 1 ảnh.");

            if (files.Count > 10)
                throw new ArgumentException("Bạn chỉ được upload tối đa 10 ảnh profile.");

            account.ProfileImages ??= new List<string>();
            var uploaded = new List<string>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;
                var url = await _photoService.UploadPhotoAsync(file, "users/profile");
                account.ProfileImages.Add(url);
                uploaded.Add(url);
            }

            _unitOfWork.Repository<Account>().Update(account);
            await _unitOfWork.CompleteAsync();
            return uploaded;
        }

        public async Task<bool> DeleteProfileImageAsync(Guid userId, string imageUrl)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("Người dùng không tồn tại.");

            account.ProfileImages ??= new List<string>();
            var existed = account.ProfileImages.FirstOrDefault(x => x == imageUrl);
            if (existed == null) return false;

            var deletedOnStorage = await _photoService.DeletePhotoAsync(imageUrl);
            if (!deletedOnStorage) return false;

            account.ProfileImages.Remove(existed);
            _unitOfWork.Repository<Account>().Update(account);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<IEnumerable<ProfileImageDTO>> GetProfileImagesAsync(Guid userId)
        {
            var account = await _unitOfWork.Repository<Account>().GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("Người dùng không tồn tại.");

            account.ProfileImages ??= new List<string>();
            return account.ProfileImages.Select(url => new ProfileImageDTO
            {
                Url = url
            }).ToList();
        }
    }
}
