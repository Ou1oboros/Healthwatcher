using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Infrastructure.Monitoring;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Tests.Infrastructure;

// The timer half of leader election: a cycle that runs longer than the lease TTL, which
// claim-once-per-cycle would let a standby steal. Real timers and real SQLite, because the
// bug is about elapsed time against a shared row.
public class HealthMonitorBackgroundServiceTests
{
    private const int IntervalSeconds = 1;
    private const int TtlSeconds = 2;

    // Longer than the TTL, so the lease expires mid-cycle unless something renews it.
    private static readonly TimeSpan CycleDuration = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task LeaderKeepsTheLeaseWhileACycleOutlastsTheTtl()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        await using ServiceProvider provider = BuildProvider(db, new SlowCycle(CycleDuration));

        HealthMonitorBackgroundService monitor = NewMonitor(provider);

        await monitor.StartAsync(CancellationToken.None);

        try
        {
            // Past the TTL, still inside the cycle.
            await Task.Delay(TimeSpan.FromSeconds(TtlSeconds * 2));

            await using AppDbContext rivalContext = db.Create();

            MonitorLeaseStore rival = new MonitorLeaseStore(
                rivalContext,
                Options.Create(new MonitoringOptions { LeaseTtlSeconds = TtlSeconds }),
                NullLogger<MonitorLeaseStore>.Instance);

            Assert.False(await rival.TryAcquireAsync("rival-pod"));
        }
        finally
        {
            await monitor.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task LeaderAbandonsTheCycleWhenItLosesTheLease()
    {
        using SqliteAppDbContextFactory db = new SqliteAppDbContextFactory();

        SlowCycle cycle = new SlowCycle(CycleDuration);

        await using ServiceProvider provider = BuildProvider(db, cycle);

        HealthMonitorBackgroundService monitor = NewMonitor(provider);

        await monitor.StartAsync(CancellationToken.None);

        try
        {
            await cycle.Started;

            // What a standby stealing the lease leaves in the row.
            await using (AppDbContext stolen = db.Create())
            {
                MonitorLease lease = await stolen.MonitorLeases.SingleAsync();
                lease.GrantTo("rival-pod", DateTimeOffset.UtcNow.AddSeconds(TtlSeconds * 10));
                await stolen.SaveChangesAsync();
            }

            // The renewal should notice and unwind the cycle.
            await cycle.Cancelled.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.False(cycle.RanToCompletion);
        }
        finally
        {
            await monitor.StopAsync(CancellationToken.None);
        }
    }

    private static HealthMonitorBackgroundService NewMonitor(ServiceProvider provider) =>
        new HealthMonitorBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new MonitoringOptions
            {
                IntervalSeconds = IntervalSeconds,
                LeaseTtlSeconds = TtlSeconds,
            }),
            NullLogger<HealthMonitorBackgroundService>.Instance);

    // Scoped over the one open connection, so every scope the service creates - including the
    // renewal's - talks to the same database.
    private static ServiceProvider BuildProvider(SqliteAppDbContextFactory db, IHealthMonitorService monitor)
    {
        ServiceCollection services = new ServiceCollection();

        services.AddLogging(logging => logging.ClearProviders());
        services.AddSingleton(Options.Create(new MonitoringOptions { LeaseTtlSeconds = TtlSeconds }));
        services.AddScoped(_ => db.Create());
        services.AddScoped<IMonitorLeaseStore, MonitorLeaseStore>();
        services.AddSingleton(monitor);

        return services.BuildServiceProvider();
    }

    // A cycle that outlasts the lease TTL, and reports how it ended.
    private sealed class SlowCycle : IHealthMonitorService
    {
        private readonly TimeSpan _duration;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SlowCycle(TimeSpan duration) => _duration = duration;

        public Task Started => _started.Task;

        public Task Cancelled => _cancelled.Task;

        public bool RanToCompletion { get; private set; }

        public async Task<int> RunCheckCycle(CancellationToken cancellationToken = default)
        {
            _started.TrySetResult();

            try
            {
                await Task.Delay(_duration, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _cancelled.TrySetResult();
                throw;
            }

            RanToCompletion = true;
            return 0;
        }
    }
}
