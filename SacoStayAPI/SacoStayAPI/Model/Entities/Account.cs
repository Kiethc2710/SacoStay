using Microsoft.AspNetCore.Identity;

namespace SacoStayAPI.Model.Entities
{
    public class Account: IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool? Gender { get; set; } // 0 female, 1 male

    }
}
