using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace SacoStayAPI.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
    {
        public ApplicationDBContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Database=sacostay;Username=postgres;Password=postgres;Port=5432";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDBContext(optionsBuilder.Options);
        }
    }
}
