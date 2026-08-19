using HealthwatcherApi.Infrastructure.Persistence;
using HealthwatcherApi.Shared.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Tests.Infrastructure;

/// <summary>
/// A throwaway SQLite database held open in memory for the life of one test.
/// Relational enough to exercise keys, indexes and query filters, without needing
/// a Postgres server. Keep the connection open — closing it drops the database.
/// </summary>
public sealed class SqliteAppDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public RequestContext RequestContext { get; }

    public SqliteAppDbContextFactory(string currentUsername = "tester")
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        RequestContext = new RequestContext { CurrentUsername = currentUsername };

        using AppDbContext context = Create();
        context.Database.EnsureCreated();
    }

    public AppDbContext Create()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, RequestContext);
    }

    public void Dispose() => _connection.Dispose();
}
