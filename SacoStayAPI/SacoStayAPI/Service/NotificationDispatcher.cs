namespace SacoStayAPI.Service
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly INotificationService _notificationService;

        public NotificationDispatcher(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public Task NotifyAsync(Guid userId, string title, string message, string type, string? linkUrl = null)
        {
            return _notificationService.CreateAsync(userId, title, message, type, linkUrl);
        }
    }
}
