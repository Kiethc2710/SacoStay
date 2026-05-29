using System;

namespace SacoStayAPI.Model.Entities
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public string PaymentMethod { get; set; } = "PayOS";
        public string? TransactionNo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? RoomPostId { get; set; }
        public string? PackageName { get; set; } // BASIC, LITE, PRO, ELITE, TENANT_PREMIUM

        // NEW: distinguish buyer target
        public string BuyerType { get; set; } = "Landlord"; // Landlord, Tenant
        public Guid? UserId { get; set; }
    }
}
