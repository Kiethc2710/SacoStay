using Microsoft.AspNetCore.Identity;
using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {

            var context = serviceProvider.GetRequiredService<ApplicationDBContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            var userManager = serviceProvider.GetRequiredService<UserManager<Account>>();

            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
        }

        private static async Task CreateUserAsync(UserManager<Account> userManager, string username, string email, string password, string firstName, string lastName, DateOnly dob, string role, bool gender)
        {
            if (await userManager.FindByNameAsync(username) == null)
            {
                var user = new Account
                {
                    UserName = username,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = dob,
                    EmailConfirmed = true,
                    Gender = gender
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
        private static async Task SeedUsersAsync(UserManager<Account> userManager)
        {
            await CreateUserAsync(
                userManager,
                username: "admin",
                email: "admin@system.com",
                password: "Admin@123",
                firstName: "System",
                lastName: "Admin",
                dob: new DateOnly(1995, 1, 1),
                role: "admin",
                gender: true
            );

            await CreateUserAsync(
                userManager,
                username: "landlord1",
                email: "landlord1@mail.com",
                password: "Landlord@123",
                firstName: "Nguyen",
                lastName: "Landlord",
                dob: new DateOnly(1990, 5, 20),
                role: "landlord",
                gender: true
            );

            await CreateUserAsync(
                userManager,
                username: "tenant1",
                email: "tenant1@mail.com",
                password: "Tenant@123",
                firstName: "Tran",
                lastName: "Tenant",
                dob: new DateOnly(2000, 8, 15),
                role: "tenants",
                gender: false
            );
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {

            var roles = new[] { "admin", "landlord", "tenants" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
                }
            }
        }
    }
}
