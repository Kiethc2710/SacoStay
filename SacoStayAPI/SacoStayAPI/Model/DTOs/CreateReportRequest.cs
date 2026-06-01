namespace SacoStayAPI.Model.DTOs
{
    public class CreateReportRequest
    {
        public Guid ReporterId { get; set; }
        public Guid? ReportedUserId { get; set; }
        public Guid? ReportedRoomId { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        // THÊM DÒNG NÀY: Client sẽ gửi danh sách file ảnh vào đây
        public List<IFormFile> Images { get; set; }
    }
}

