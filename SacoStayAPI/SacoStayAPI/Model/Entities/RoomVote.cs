namespace SacoStayAPI.Model.Entities
{
    public class RoomVote
    {
        public Guid Id { get; set; }
        public Guid ShortlistId { get; set; }
        public Guid UserId { get; set; }
        public string VoteStatus { get; set; } = "Like"; // Like, Dislike
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public virtual SpaceShortlist Shortlist { get; set; } = null!;
    }
}
