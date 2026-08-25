using Enums;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Infrastructure.Persistence;
using HealthwatcherApi.Infrastructure.Persistence.Repositories;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Tests.Infrastructure;

// The uptime numbers are counted by the database, so what breaks is the LINQ-to-SQL
// translation - which a substituted DbContext would never catch. Hence real SQLite.
public class TargetRepositoryTests
{
    private static async Task<Target> SeedTarget(AppDbContext context)
    {
        Target target = new Target("google", "https://google.com/", new HealthCheckRecord());
        context.Targets.Add(target);
        await context.SaveChangesAsync();

        return target;
    }

    private static void AddCheck(AppDbContext context, Target target, ConnectionStatus status, DateTimeOffset checkedAt)
    {
        HealthCheckRecord record = status == ConnectionStatus.Up
            ? HealthCheckRecord.Up(200, 42)
            : HealthCheckRecord.Down(503, null, "service unavailable");

        context.TargetHistory.Add(new TargetHistory(target, record, checkedAt));
    }

    [Fact]
    public async Task GetCheckCounts_SplitsTheWindowIntoUpChecksAndTotalChecks()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Target target = await SeedTarget(context);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        AddCheck(context, target, ConnectionStatus.Up, now.AddMinutes(-30));
        AddCheck(context, target, ConnectionStatus.Up, now.AddMinutes(-20));
        AddCheck(context, target, ConnectionStatus.Down, now.AddMinutes(-10));
        await context.SaveChangesAsync();

        (int upCount, int totalCount) =
            await new TargetRepository(context).GetCheckCountsAsync(target.Id, now.AddHours(-1));

        Assert.Equal(2, upCount);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetCheckCounts_IgnoresChecksOlderThanTheWindowAndOtherTargets()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Target target = await SeedTarget(context);
        Target other = new Target("bing", "https://bing.com/", new HealthCheckRecord());
        context.Targets.Add(other);
        await context.SaveChangesAsync();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AddCheck(context, target, ConnectionStatus.Up, now.AddMinutes(-5));
        AddCheck(context, target, ConnectionStatus.Up, now.AddHours(-3)); // before the window
        AddCheck(context, other, ConnectionStatus.Up, now.AddMinutes(-5)); // different target
        await context.SaveChangesAsync();

        (int upCount, int totalCount) =
            await new TargetRepository(context).GetCheckCountsAsync(target.Id, now.AddHours(-1));

        Assert.Equal(1, upCount);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public async Task GetCheckCounts_ReturnsZerosWhenTheWindowHoldsNoChecks()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Target target = await SeedTarget(context);

        (int upCount, int totalCount) = await new TargetRepository(context)
            .GetCheckCountsAsync(target.Id, DateTimeOffset.UtcNow.AddHours(-1));

        Assert.Equal(0, upCount);
        Assert.Equal(0, totalCount);
    }
}
