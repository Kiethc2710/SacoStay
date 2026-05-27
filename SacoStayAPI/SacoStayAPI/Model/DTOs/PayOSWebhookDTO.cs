namespace SacoStayAPI.Model.DTOs
{
    public class PayOSWebhookDTO
    {
        public long OrderCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? Code { get; set; }
        public string? Desc { get; set; }
    }
}
