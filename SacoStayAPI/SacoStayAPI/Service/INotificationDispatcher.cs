namespace SacoStayAPI.Service
{
    public interface INotificationDispatcher
    {
        Task NotifyAsync(Guid userId, string title, string message, string type, string? linkUrl = null);
    }
}
