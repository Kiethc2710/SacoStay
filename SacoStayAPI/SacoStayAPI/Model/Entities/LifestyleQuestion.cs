namespace SacoStayAPI.Model.Entities
{
    public class LifestyleQuestion
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public List<LifestyleOption> Options { get; set; } = new List<LifestyleOption>();
    }
}
