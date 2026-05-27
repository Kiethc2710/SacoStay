namespace SacoStayAPI.Model.DTOs
{
    public class SwipeQuotaDTO
    {
        public bool IsPremium { get; set; }
        public int? WeeklyLimit { get; set; }
        public int UsedThisWeek { get; set; }
        public int? Remaining { get; set; }
        public DateTime WeekResetAt { get; set; }
    }
}
