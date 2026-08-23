using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Infrastructure.Persistence;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Tests.Infrastructure;

/// <summary>
/// The DbContext carries behaviour of its own — audit stamping and the global
/// soft-delete filter. Both fail silently if broken, so they get real coverage.
/// </summary>
public class AppDbContextTests
{
    private static Target NewTarget(string name = "google") =>
        new Target(name, "https://google.com/", new HealthCheckRecord());

    [Fact]
    public async Task SaveChanges_StampsAuditFieldsOnInsert()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory("musab");
        await using AppDbContext context = db.Create();

        Target target = NewTarget();
        context.Targets.Add(target);
        await context.SaveChangesAsync();

        Assert.Equal("musab", target.CreatedBy);
        Assert.Equal("musab", target.UpdatedBy);
        Assert.NotEqual(default, target.CreatedAt);
        Assert.Equal(target.CreatedAt, target.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_FallsBackToSystemWhenNobodyIsAuthenticated()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory(currentUsername: null);
        await using AppDbContext context = db.Create();

        context.Targets.Add(NewTarget());
        await context.SaveChangesAsync();

        Assert.Equal("system", (await context.Targets.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_MovesUpdatedAtButNotCreatedAtOnEdit()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Target target = NewTarget();
        context.Targets.Add(target);
        await context.SaveChangesAsync();
        DateTimeOffset createdAt = target.CreatedAt;

        target.Rename("renamed");
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, target.CreatedAt);
        Assert.True(target.UpdatedAt >= createdAt);
    }

    [Fact]
    public async Task QueryFilter_HidesSoftDeletedRows()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        Guid targetId;
        await using (AppDbContext seed = db.Create())
        {
            Target target = NewTarget();
            seed.Targets.Add(target);
            await seed.SaveChangesAsync();
            targetId = target.Id;

            target.IsDeleted = true;
            await seed.SaveChangesAsync();
        }

        await using AppDbContext verify = db.Create();

        Assert.Empty(await verify.Targets.ToListAsync());
        Assert.Equal(targetId, (await verify.Targets.IgnoreQueryFilters().SingleAsync()).Id);
    }

    [Fact]
    public async Task OwnedHealthCheckRecord_RoundTripsThroughTheDatabase()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        Guid targetId;
        await using (AppDbContext seed = db.Create())
        {
            Target target = NewTarget();
            target.RecordCheck(HealthCheckRecord.Up(200, 12.5), DateTimeOffset.UtcNow);
            seed.Targets.Add(target);
            await seed.SaveChangesAsync();
            targetId = target.Id;
        }

        await using AppDbContext verify = db.Create();
        Target reloaded = await verify.Targets.SingleAsync(t => t.Id == targetId);

        Assert.Equal(200, reloaded.HealthCheckRecord.StatusCode);
        Assert.Equal(12.5, reloaded.HealthCheckRecord.ResponseTimeMs);
    }
}
