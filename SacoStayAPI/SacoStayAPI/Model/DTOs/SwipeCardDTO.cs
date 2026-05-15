namespace SacoStayAPI.Model.DTOs
{
    public class SwipeCardDTO
    {
        public string UserId { get; set; } // Frontend cần cái này để hiện Avatar, Tên...
        public int MatchingScore { get; set; } // Điểm hợp nhau hiển thị lên thẻ
    }
}
