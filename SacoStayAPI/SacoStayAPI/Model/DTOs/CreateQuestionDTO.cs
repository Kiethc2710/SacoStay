namespace SacoStayAPI.Model.DTOs
{
    public class CreateQuestionDTO
    {
        public string Content { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
    }
}
