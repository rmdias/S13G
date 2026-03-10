using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace S13G.Infrastructure.Persistence
{
    // Design-time factory used by EF Core tools to create a context without
    // building the full application host. This avoids startup-time errors such
    // as attempting to resolve hosted services that depend on scoped services.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // build connection string from environment variables, falling back to current user
            var user = Environment.GetEnvironmentVariable("DB_USER");
            if (string.IsNullOrWhiteSpace(user))
            {
                user = Environment.UserName; // on macOS Homebrew postgres defaults to current account
            }

            var password = Environment.GetEnvironmentVariable("DB_PASS") ?? string.Empty;

            var conn = $"Host=localhost;Port=5432;Database=s13g;Username={user};" +
                       (string.IsNullOrEmpty(password) ? string.Empty : $"Password={password};");

            optionsBuilder.UseNpgsql(conn);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
