namespace SacoStayAPI.Model.Entities
{
    public class UserSwipe
    {
        public int Id { get; set; }

        public string SwiperId { get; set; } // ID của người đang lướt app
        public string SwipedUserId { get; set; } // ID của người bị lướt (mặt trên cái thẻ)

        public bool IsLike { get; set; } // True = Quẹt phải (Thích), False = Quẹt trái (Bỏ qua)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Thời gian quẹt
    }
}
