using Microsoft.AspNetCore.Http;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public class TenantRoomProfileService : ITenantRoomProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService _photoService;
        private const int MAX_IMAGES = 10;
        private const string FOLDER_NAME = "tenant-rooms";

        public TenantRoomProfileService(IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
        }

        public async Task<TenantRoomProfileDTO?> GetByUserIdAsync(string userId)
        {
            var profile = await _unitOfWork.Repository<TenantRoomProfile>().GetByIdAsync(Guid.Parse(userId));
            if (profile == null) return null;
            return MapToDTO(profile);
        }

        public async Task<TenantRoomProfileDTO> CreateAsync(string userId, CreateTenantRoomProfileDTO dto)
        {
            var userGuid = Guid.Parse(userId);
            var existing = await _unitOfWork.Repository<TenantRoomProfile>().GetByIdAsync(userGuid);
            if (existing != null)
            {
                throw new InvalidOperationException("Hồ sơ phòng đã tồn tại. Vui lòng sử dụng API cập nhật.");
            }

            var profile = new TenantRoomProfile
            {
                UserId = userGuid,
                City = dto.City,
                District = dto.District,
                MaxPeople = dto.MaxPeople,
                Amenities = dto.Amenities ?? new List<string>(),
                ExtraNotes = dto.ExtraNotes,
                Price = dto.Price,
                Images = new List<string>(),
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<TenantRoomProfile>().AddAsync(profile);
            await _unitOfWork.CompleteAsync();

            return MapToDTO(profile);
        }

        public async Task<TenantRoomProfileDTO> UpdateAsync(string userId, UpdateTenantRoomProfileDTO dto)
        {
            var userGuid = Guid.Parse(userId);
            var profile = await _unitOfWork.Repository<TenantRoomProfile>().GetByIdAsync(userGuid);
            if (profile == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ phòng của bạn.");
            }

            if (dto.City != null) profile.City = dto.City;
            if (dto.District != null) profile.District = dto.District;
            if (dto.MaxPeople.HasValue) profile.MaxPeople = dto.MaxPeople;
            if (dto.Amenities != null) profile.Amenities = dto.Amenities;
            if (dto.ExtraNotes != null) profile.ExtraNotes = dto.ExtraNotes;
            if (dto.Price.HasValue) profile.Price = dto.Price;
            if (dto.Images != null) profile.Images = dto.Images;
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();
            return MapToDTO(profile);
        }

        public async Task<TenantRoomProfileDTO> UploadImagesAsync(string userId, List<IFormFile> files)
        {
            var userGuid = Guid.Parse(userId);
            var profile = await _unitOfWork.Repository<TenantRoomProfile>().GetByIdAsync(userGuid);
            if (profile == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ phòng của bạn.");
            }

            if (files == null || files.Count == 0)
            {
                throw new ArgumentException("Danh sách file không hợp lệ.");
            }

            var currentCount = profile.Images?.Count ?? 0;
            if (currentCount + files.Count > MAX_IMAGES)
            {
                throw new ArgumentException($"Số lượng ảnh không được vượt quá {MAX_IMAGES}. Hiện tại bạn có {currentCount} ảnh, có thể thêm tối đa {MAX_IMAGES - currentCount} ảnh.");
            }

            var uploadedUrls = new List<string>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var url = await _photoService.UploadPhotoAsync(file, FOLDER_NAME);
                    uploadedUrls.Add(url);
                }
            }

            if (profile.Images == null)
            {
                profile.Images = new List<string>();
            }
            profile.Images.AddRange(uploadedUrls);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();
            return MapToDTO(profile);
        }

        public async Task<TenantRoomProfileDTO> DeleteImageAsync(string userId, string imageUrl)
        {
            var userGuid = Guid.Parse(userId);
            var profile = await _unitOfWork.Repository<TenantRoomProfile>().GetByIdAsync(userGuid);
            if (profile == null)
            {
                throw new KeyNotFoundException("Không tìm thấy hồ sơ phòng của bạn.");
            }

            if (profile.Images == null || !profile.Images.Contains(imageUrl))
            {
                throw new ArgumentException("Ảnh không tồn tại trong hồ sơ của bạn.");
            }

            await _photoService.DeletePhotoAsync(imageUrl);
            profile.Images.Remove(imageUrl);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();
            return MapToDTO(profile);
        }

        private static TenantRoomProfileDTO MapToDTO(TenantRoomProfile profile)
        {
            return new TenantRoomProfileDTO
            {
                UserId = profile.UserId,
                City = profile.City,
                District = profile.District,
                MaxPeople = profile.MaxPeople,
                Amenities = profile.Amenities ?? new List<string>(),
                ExtraNotes = profile.ExtraNotes,
                Price = profile.Price,
                Images = profile.Images ?? new List<string>(),
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}
