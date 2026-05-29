namespace SacoStayAPI.Model.DTOs
{
    public class TenantPackageDTO
    {
        public string PackageType { get; set; } = "Free";
        public DateTime? ExpiresAt { get; set; }
        public bool IsPremium => PackageType.Equals("Premium", StringComparison.OrdinalIgnoreCase)
                                 && ExpiresAt.HasValue
                                 && ExpiresAt.Value > DateTime.UtcNow;
    }
}
