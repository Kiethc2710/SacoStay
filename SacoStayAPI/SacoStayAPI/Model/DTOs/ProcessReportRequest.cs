namespace SacoStayAPI.Model.DTOs
{
    public class ProcessReportRequest
    {
        // true = Report đúng sự thật (Approved) | false = Report sai (Rejected)
        public bool IsValid { get; set; }
        public string? AdminNote { get; set; }
    }
}
