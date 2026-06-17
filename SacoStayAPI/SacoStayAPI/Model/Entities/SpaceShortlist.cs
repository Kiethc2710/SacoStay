namespace SacoStayAPI.Model.Entities
{
    public class SpaceShortlist
    {
        public Guid Id { get; set; }
        public Guid SpaceId { get; set; }
        public Guid RoomId { get; set; }
        public Guid AddedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Để dùng lệnh .Include trong EF Core)
        public virtual SharedSpace Space { get; set; } = null!;
        public virtual RoomPost Room { get; set; } = null!;

        // Quan hệ: 1 phòng trong Shortlist có thể nhận nhiều lượt Vote từ 2 user
        public virtual ICollection<RoomVote> Votes { get; set; } = new List<RoomVote>();
    }
}
