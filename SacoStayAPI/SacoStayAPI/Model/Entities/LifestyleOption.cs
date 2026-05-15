namespace SacoStayAPI.Model.Entities
{
    public class LifestyleOption
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;
        public int LifestyleQuestionId { get; set; }

        public LifestyleQuestion LifestyleQuestion { get; set; }
    }
}
