using System.ComponentModel.DataAnnotations;

namespace SacoStayAPI.Model.Entities
{
    public enum KycStatus
    {
        Pending,
        Approved,
        Rejected,
        NeedReupload
    }
    public class KycRequest
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string NationalId { get; set; }
        public string FrontIdImageUrl { get; set; }
        public string BackIdImageUrl { get; set; }
        public string SelfieImageUrl { get; set; }
        public string? VneidScreenshotUrl { get; set; }
        public KycStatus Status { get; set; } = KycStatus.Pending;
        public string? AdminNote { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
