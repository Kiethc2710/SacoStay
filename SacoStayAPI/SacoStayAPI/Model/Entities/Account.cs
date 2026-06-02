using Microsoft.AspNetCore.Identity;

namespace SacoStayAPI.Model.Entities
{
    public class Account : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool? Gender { get; set; }

        public string? Job { get; set; }
        public string? LivingArea { get; set; }
        public string? Bio { get; set; }
        public List<string>? ProfileImages { get; set; } = new List<string>();

        // Tenant package
        public string TenantPackageType { get; set; } = "Free"; // Free, Premium
        public DateTime? TenantPackageExpiresAt { get; set; }
    }
}
