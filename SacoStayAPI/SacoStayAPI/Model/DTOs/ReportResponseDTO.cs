namespace SacoStayAPI.Model.DTOs
{
    public class ReportResponseDTO
    {
        public Guid ReportId { get; set; }
        public string ReporterName { get; set; }
        public Guid? ReportedUserId { get; set; }
        public string? ReportedUserName { get; set; }
        public Guid? ReportedRoomId { get; set; }
        public string? ReportedRoomName { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string>? Images { get; set; } // THÊM DÒNG NÀY
    }
}
