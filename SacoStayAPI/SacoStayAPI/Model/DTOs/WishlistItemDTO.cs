namespace SacoStayAPI.Model.DTOs
{
    public class WishlistItemDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int MatchingScore { get; set; }
        public DateTime LikedAt { get; set; }
    }
}
