using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Tests.Infrastructure;

/// <summary>
/// The DbContext carries behaviour of its own — audit stamping and the global
/// soft-delete filter. Both fail silently if broken, so they get real coverage.
/// </summary>
public class AppDbContextTests
{
    [Fact]
    public async Task SaveChanges_StampsAuditFieldsOnInsert()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory("musab");
        await using AppDbContext context = db.Create();

        Target target = new Target("google");
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
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        db.RequestContext.CurrentUsername = null;
        await using AppDbContext context = db.Create();

        context.Targets.Add(new Target("google"));
        await context.SaveChangesAsync();

        Assert.Equal("system", (await context.Targets.SingleAsync()).CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_MovesUpdatedAtButNotCreatedAtOnEdit()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Target target = new Target("google");
        context.Targets.Add(target);
        await context.SaveChangesAsync();
        DateTime createdAt = target.CreatedAt;

        target.Name = "renamed";
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
            Target target = new Target("google");
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
    public Task QueryFilter_AppliesToEveryEntityDerivedFromBaseEntity() => throw new NotImplementedException();
}
