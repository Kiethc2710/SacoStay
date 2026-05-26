namespace SacoStayAPI.Model.DTOs
{
    public class UserLifestyleAnswerDTO
    {
        public int QuestionId { get; set; }
        public string QuestionContent { get; set; } = string.Empty;
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
    }
}
