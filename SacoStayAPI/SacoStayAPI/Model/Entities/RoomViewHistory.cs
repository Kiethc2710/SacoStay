using System;

namespace SacoStayAPI.Model.Entities
{
    public class RoomViewHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoomPostId { get; set; }
        public Guid TenantId { get; set; } // ID của người thuê trọ bấm xem tin
        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties để sau này dùng Include lấy thông tin tên, avatar người xem
        public virtual RoomPost? RoomPost { get; set; }
        public virtual Account? Tenant { get; set; }
    }
}