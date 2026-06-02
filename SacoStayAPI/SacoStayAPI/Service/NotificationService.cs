using Microsoft.AspNetCore.SignalR;
using SacoStayAPI.Hubs;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(IUnitOfWork unitOfWork, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<NotificationDTO> CreateAsync(Guid userId, string title, string message, string type, string? linkUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                LinkUrl = linkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Notification>().AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            var dto = MapToDto(notification);
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", dto);

            return dto;
        }

        public async Task<List<NotificationDTO>> GetMyNotificationsAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var notifications = (await _unitOfWork.Repository<Notification>()
                    .FindAsync(x => x.UserId == userId))
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return notifications;
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            var notifications = await _unitOfWork.Repository<Notification>()
                .FindAsync(x => x.UserId == userId && !x.IsRead);

            return notifications.Count();
        }

        public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var items = await _unitOfWork.Repository<Notification>()
                .FindAsync(x => x.UserId == userId && x.Id == notificationId);

            var notification = items.FirstOrDefault();
            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _unitOfWork.Repository<Notification>().Update(notification);
                await _unitOfWork.CompleteAsync();
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var items = (await _unitOfWork.Repository<Notification>()
                .FindAsync(x => x.UserId == userId && !x.IsRead)).ToList();

            foreach (var notification in items)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                _unitOfWork.Repository<Notification>().Update(notification);
            }

            if (items.Any())
                await _unitOfWork.CompleteAsync();

            return items.Count;
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid notificationId)
        {
            var items = await _unitOfWork.Repository<Notification>()
                .FindAsync(x => x.UserId == userId && x.Id == notificationId);

            var notification = items.FirstOrDefault();
            if (notification == null) return false;

            _unitOfWork.Repository<Notification>().Remove(notification);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        private static NotificationDTO MapToDto(Notification notification)
        {
            return new NotificationDTO
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                LinkUrl = notification.LinkUrl,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }
    }
}
