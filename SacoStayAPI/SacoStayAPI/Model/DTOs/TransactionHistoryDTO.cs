namespace SacoStayAPI.Model.DTOs
{
    public class TransactionHistoryDTO
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionNo { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? RoomPostId { get; set; }
        public string? RoomTitle { get; set; }
        public string? PackageName { get; set; }
    }
}
