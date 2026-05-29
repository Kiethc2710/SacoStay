namespace SacoStayAPI.Model.DTOs
{
    public class PackagePaymentRequestDTO
    {
        public Guid? RoomPostId { get; set; }
        public Guid? UserId { get; set; }
        public string PackageName { get; set; } = string.Empty;
    }
}
