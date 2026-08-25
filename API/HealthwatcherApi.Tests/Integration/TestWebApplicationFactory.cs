using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthwatcherApi.Tests.Integration;

// Boots the real application - real middleware, routing and DI graph - and swaps only the
// database for an in-memory SQLite one.
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new SqliteConnection("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // All four: EF refuses to run with two providers registered for one context, and
            // leaving any behind keeps the appsettings-configured one alive.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_connection)
                .UseSnakeCaseNamingConvention());

            // appsettings.json points Monitoring:Targets at real hosts; a test run must not probe them.
            services.PostConfigure<MonitoringOptions>(options => options.Targets = []);
        });
    }

    /// <summary>Runs an action against a scoped DbContext, to seed or assert.</summary>
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
