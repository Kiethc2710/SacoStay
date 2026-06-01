namespace SacoStayAPI.Model.Entities
{
    public class Report
    {
    public Guid ReportId { get; set; } = Guid.NewGuid();
    
    // Người thực hiện report
    public Guid ReporterId { get; set; }
    public Account Reporter { get; set; }

    // Người bị report (Có thể null nếu chỉ report phòng)
    public Guid? ReportedUserId { get; set; }
    public Account? ReportedUser { get; set; }

    // Phòng bị report (Có thể null nếu chỉ report user)
    public Guid? ReportedRoomId { get; set; }
    public RoomPost? ReportedRoom { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Trạng thái xử lý (Pending, Reviewed, Resolved, Rejected)
    public string Status { get; set; } = "Pending";
}
}
