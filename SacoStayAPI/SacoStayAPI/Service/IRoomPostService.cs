using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public interface IRoomPostService
    {
        Task<RoomPost> CreatePostAsync(CreateRoomPostDTO dto, Guid userId);
        Task<IEnumerable<RoomPost>> GetMyPostsAsync(Guid userId);
        Task<IEnumerable<object>> GetRoomsNearbyAsync(double userLat, double userLng, double radiusInKm);

        Task RecordViewAsync(Guid roomPostId, Guid tenantId);
        Task<object> GetRoomAnalyticsAsync(Guid roomPostId, Guid userId);
        Task<RoomPost> UpdateRoomPostStatusAsync(Guid roomPostId, Guid userId, string status);
    }
}