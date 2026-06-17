namespace SacoStayAPI.Model.Entities
{
    public class SharedSpace
    {
        public Guid Id { get; set; }
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
        public string Status { get; set; } = "Active"; // Active, Finalized, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? FinalizedRoomId { get; set; }
        public Guid? FinalizeRequestedByUserId { get; set; }

        // Quan hệ: 1 Không gian chung có nhiều phòng trong danh sách Shortlist
        public virtual ICollection<SpaceShortlist> Shortlists { get; set; } = new List<SpaceShortlist>();
    }
}
