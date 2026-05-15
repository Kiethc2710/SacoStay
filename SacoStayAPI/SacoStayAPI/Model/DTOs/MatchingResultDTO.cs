namespace SacoStayAPI.Model.DTOs
{
    public class MatchingResultDTO
    {
        public string TargetUserId { get; set; }
        public int MatchingScore { get; set; }
        public int TotalQuestions { get; set; }
        public int MatchedAnswers { get; set; }
    }
}
