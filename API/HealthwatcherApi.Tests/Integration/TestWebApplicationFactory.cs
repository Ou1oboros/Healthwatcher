using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthwatcherApi.Tests.Integration;

/// <summary>
/// Boots the real application — real middleware, real routing, real DI graph —
/// and swaps only the database for an in-memory SQLite one. If Program.cs or the
/// service registrations break, these tests fail rather than production.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // All three matter: EF Core 9 keeps the provider registration in
            // IDbContextOptionsConfiguration, and leaving it behind means Npgsql
            // and Sqlite are both registered — which EF refuses to run.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_connection)
                .UseSnakeCaseNamingConvention());
        });
    }

    /// <summary>Runs an action against a scoped DbContext — use it to seed or assert.</summary>
    public async Task<T> WithDbContext<T>(Func<AppDbContext, Task<T>> action)
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        return await action(context);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
