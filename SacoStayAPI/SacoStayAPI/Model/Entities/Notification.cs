using System;

namespace SacoStayAPI.Model.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "general"; // general, chat, payment, system
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }

        public virtual Account? User { get; set; }
    }
}
