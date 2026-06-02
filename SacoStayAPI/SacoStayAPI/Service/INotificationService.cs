using SacoStayAPI.Model.DTOs;

namespace SacoStayAPI.Service
{
    public interface INotificationService
    {
        Task<NotificationDTO> CreateAsync(Guid userId, string title, string message, string type, string? linkUrl = null);
        Task<List<NotificationDTO>> GetMyNotificationsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<int> MarkAllAsReadAsync(Guid userId);
        Task<bool> DeleteAsync(Guid userId, Guid notificationId);
    }
}
