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
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<LifestyleQuestion> LifestyleQuestions { get; set; }

        public DbSet<LifestyleOption> LifestyleOptions { get; set; }
        public DbSet<UserLifestyle> UserLifestyles { get; set; }
        public DbSet<UserSwipe> UserSwipes { get; set; }
        public DbSet<RoomPost> RoomPosts { get; set; }
        public DbSet<RoomViewHistory> RoomViewHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<KycRequest> KycSubmissions { get; set; }
        public DbSet<SharedSpace> SharedSpaces { get; set; }
        public DbSet<SpaceShortlist> SpaceShortlists { get; set; }
        public DbSet<RoomVote> RoomVotes { get; set; }
        public DbSet<TenantRoomProfile> TenantRoomProfiles { get; set; }
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
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id); 

                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict); // Không xóa tin nhắn khi xóa user để giữ lịch sử chat
            });
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            });
            ////Seed roles với GUID cố định
            //var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            //var tenantsRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            //var landlordRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            //modelBuilder.Entity<IdentityRole<Guid>>().HasData(
            //    new IdentityRole<Guid> { Id = adminRoleId, Name = "admin", NormalizedName = "ADMIN" },
            //    new IdentityRole<Guid> { Id = tenantsRoleId, Name = "tenants", NormalizedName = "TENANTS" },
            //    new IdentityRole<Guid> { Id = landlordRoleId, Name = "landlord", NormalizedName = "LANDLORD" }
            //);
            //tạo giá trị mặc định cho CreatedAt cho Account
            //modelBuilder.Entity<Account>(entity =>
            //{
            //    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            //});
            // tạo giá trị mặc định cho CreatedAt cho Account
            modelBuilder.Entity<Account>(entity =>
            {
                // ĐỔI GETUTCDATE() (SQL Server) thành now() (PostgreSQL)
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            });

            // Thêm đoạn này để tránh lỗi 'Amount' của PaymentTransaction luôn nhé
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // TenantRoomProfile configuration
            modelBuilder.Entity<TenantRoomProfile>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
                entity.HasOne(e => e.User)
                      .WithOne()
                      .HasForeignKey<TenantRoomProfile>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
