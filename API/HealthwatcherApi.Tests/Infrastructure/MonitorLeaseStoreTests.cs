using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HealthwatcherApi.Tests.Infrastructure;

// The lease keeps a scaled-out deployment from probing every target once per replica, and
// its failure mode - two leaders - is silent. Real SQLite, because the guarantee being
// tested is the concurrency token in the generated UPDATE.
public class MonitorLeaseStoreTests
{
    private const int TtlSeconds = 90;

    private static MonitorLeaseStore NewStore(AppDbContext context, int ttlSeconds = TtlSeconds) =>
        new MonitorLeaseStore(
            context,
            Options.Create(new MonitoringOptions { LeaseTtlSeconds = ttlSeconds }),
            Substitute.For<ILogger<MonitorLeaseStore>>());

    private static async Task GrantTo(AppDbContext context, string owner, DateTimeOffset expiresAt)
    {
        MonitorLease lease = await context.MonitorLeases.SingleAsync();
        lease.GrantTo(owner, expiresAt);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task TryAcquire_ClaimsTheLeaseNobodyHolds()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext context = db.Create();

        Assert.True(await NewStore(context).TryAcquireAsync("pod-a"));

        MonitorLease lease = await context.MonitorLeases.SingleAsync();
        Assert.Equal("pod-a", lease.Owner);
        Assert.True(lease.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TryAcquire_RefusesWhileAnotherReplicaLeaseIsStillValid()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        await using (AppDbContext held = db.Create())
            await GrantTo(held, "pod-a", DateTimeOffset.UtcNow.AddSeconds(TtlSeconds));

        await using AppDbContext context = db.Create();

        Assert.False(await NewStore(context).TryAcquireAsync("pod-b"));
        Assert.Equal("pod-a", (await context.MonitorLeases.SingleAsync()).Owner);
    }

    [Fact]
    public async Task TryAcquire_TakesOverOnceTheHolderLeaseHasExpired()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        // What a pod SIGKILLed while leading leaves behind.
        await using (AppDbContext expired = db.Create())
            await GrantTo(expired, "pod-a", DateTimeOffset.UtcNow.AddSeconds(-1));

        await using AppDbContext context = db.Create();

        Assert.True(await NewStore(context).TryAcquireAsync("pod-b"));
        Assert.Equal("pod-b", (await context.MonitorLeases.SingleAsync()).Owner);
    }

    [Fact]
    public async Task TryAcquire_PushesTheExpiryOutWhenTheHolderRenews()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        await using (AppDbContext nearlyExpired = db.Create())
            await GrantTo(nearlyExpired, "pod-a", DateTimeOffset.UtcNow.AddSeconds(1));

        await using AppDbContext context = db.Create();

        Assert.True(await NewStore(context).TryAcquireAsync("pod-a"));
        Assert.True((await context.MonitorLeases.SingleAsync()).ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(60));
    }

    [Fact]
    public async Task TryAcquire_LetsOnlyOneOfTwoReplicasReachingForTheSameLeaseWin()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();
        await using AppDbContext contextA = db.Create();
        await using AppDbContext contextB = db.Create();

        // Both read the free lease before either writes. Without the token, both would win.
        await contextA.MonitorLeases.LoadAsync();
        await contextB.MonitorLeases.LoadAsync();

        Assert.True(await NewStore(contextA).TryAcquireAsync("pod-a"));
        Assert.False(await NewStore(contextB).TryAcquireAsync("pod-b"));

        await using AppDbContext verify = db.Create();
        Assert.Equal("pod-a", (await verify.MonitorLeases.SingleAsync()).Owner);
    }

    [Fact]
    public async Task Release_HandsTheLeaseOverWithoutWaitingOutTheTtl()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        await using (AppDbContext leader = db.Create())
            Assert.True(await NewStore(leader).TryAcquireAsync("pod-a"));

        await using (AppDbContext blocked = db.Create())
            Assert.False(await NewStore(blocked).TryAcquireAsync("pod-b"));

        await using (AppDbContext shuttingDown = db.Create())
            await NewStore(shuttingDown).ReleaseAsync("pod-a");

        await using AppDbContext successor = db.Create();
        Assert.True(await NewStore(successor).TryAcquireAsync("pod-b"));
    }

    [Fact]
    public async Task Release_IgnoresAReplicaThatIsNotTheHolder()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        await using (AppDbContext leader = db.Create())
            Assert.True(await NewStore(leader).TryAcquireAsync("pod-a"));

        await using (AppDbContext other = db.Create())
            await NewStore(other).ReleaseAsync("pod-b");

        await using AppDbContext context = db.Create();
        MonitorLease lease = await context.MonitorLeases.SingleAsync();

        Assert.Equal("pod-a", lease.Owner);
        Assert.True(lease.ExpiresAt > DateTimeOffset.UtcNow);
    }
}
