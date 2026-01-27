using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Data
{
    public class ApplicationDBContext: IdentityDbContext<Account,IdentityRole<Guid>,Guid>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // đừng quên gọi base để Identity hoạt động đúng
            // Đổi tên bảng Identity mặc định, không override property
            modelBuilder.Entity<Account>().ToTable("Accounts");
            modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            //// Seed roles với GUID cố định
            //var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            //var staffRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            //var consultantRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            //var managerRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
            //var customerRoleId = Guid.Parse("55555555-5555-5555-5555-555555555555");
            //modelBuilder.Entity<IdentityRole<Guid>>().HasData(
            //    new IdentityRole<Guid> { Id = adminRoleId, Name = "admin", NormalizedName = "ADMIN" },
            //    new IdentityRole<Guid> { Id = staffRoleId, Name = "staff", NormalizedName = "STAFF" },
            //    new IdentityRole<Guid> { Id = consultantRoleId, Name = "consultant", NormalizedName = "CONSULTANT" },
            //    new IdentityRole<Guid> { Id = managerRoleId, Name = "manager", NormalizedName = "MANAGER" },
            //    new IdentityRole<Guid> { Id = customerRoleId, Name = "customer", NormalizedName = "CUSTOMER" }
            //);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}
