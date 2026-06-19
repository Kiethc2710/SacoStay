using SacoStayAPI.Model.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public interface ITenantRoomProfileService
    {
        Task<TenantRoomProfileDTO?> GetByUserIdAsync(string userId);
        Task<TenantRoomProfileDTO> CreateAsync(string userId, CreateTenantRoomProfileDTO dto);
        Task<TenantRoomProfileDTO> UpdateAsync(string userId, UpdateTenantRoomProfileDTO dto);
        Task<TenantRoomProfileDTO> UploadImagesAsync(string userId, List<Microsoft.AspNetCore.Http.IFormFile> files);
        Task<TenantRoomProfileDTO> DeleteImageAsync(string userId, string imageUrl);
    }
}
