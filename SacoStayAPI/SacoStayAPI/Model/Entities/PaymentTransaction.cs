namespace SacoStayAPI.Model.Entities
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        public string OrderId { get; set; }

        public decimal Amount { get; set; }

        public string Status { get; set; } // Pending, Success, Failed

        public string PaymentMethod { get; set; } // VNPay

        public string? TransactionNo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
