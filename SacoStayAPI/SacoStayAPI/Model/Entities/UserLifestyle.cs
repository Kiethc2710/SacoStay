namespace SacoStayAPI.Model.Entities
{
    public class UserLifestyle
    {
        public int Id { get; set; }
        public string UserId { get; set; } // ID của User (thường là string nếu dùng Identity)

        public int LifestyleOptionId { get; set; }
        public LifestyleOption LifestyleOption { get; set; }

        // Thêm trường này để dễ truy vấn theo câu hỏi nếu cần
        public int LifestyleQuestionId { get; set; }
        public LifestyleQuestion LifestyleQuestion { get; set; }
    }
}
