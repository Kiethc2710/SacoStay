namespace SacoStayAPI.Model.DTOs
{
    public class LifestyleQuestionDTO
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public List<LifestyleOptionDTO> Options { get; set; }
    }
}
