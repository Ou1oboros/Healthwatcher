using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Monitoring;

// Owns the timer, not the work. Every replica runs this, but a cycle only goes ahead on
// whichever pod holds the monitor lease, so scaling out doesn't multiply the probes.
public class HealthMonitorBackgroundService : BackgroundService
{
    // Machine name is the pod name in Kubernetes; the pid keeps it unique when two
    // instances share a host, as they do when running the API twice locally.
    private static readonly string Owner = $"{Environment.MachineName}/{Environment.ProcessId}";

    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<HealthMonitorBackgroundService> _logger;

    // null until the first lease attempt, so the first tick always logs a role.
    private bool? _isLeader;

    public HealthMonitorBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<HealthMonitorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
        _logger.LogInformation(
            "Health monitor starting on {Owner}, checking every {Interval} whenever it holds the lease",
            Owner, interval);

        using PeriodicTimer timer = new PeriodicTimer(interval);

        try
        {
            // run once up front instead of waiting out the first interval
            do
            {
                await RunCycleSafely(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Health monitor stopping");
        }
        finally
        {
            await ReleaseLease();
        }
    }

    // A bad cycle (dropped DB connection, a buggy probe) must not kill monitoring for the pod.
    private async Task RunCycleSafely(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            IMonitorLeaseStore leaseStore = scope.ServiceProvider.GetRequiredService<IMonitorLeaseStore>();

            if (!await leaseStore.TryAcquireAsync(Owner, cancellationToken))
            {
                SetLeader(false);
                return;
            }

            SetLeader(true);

            // Cancelled by shutdown, or by the renewal below if the lease is ever lost.
            using CancellationTokenSource cycle =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task renewal = RenewLeaseDuringCycle(cycle);

            IHealthMonitorService monitor = scope.ServiceProvider.GetRequiredService<IHealthMonitorService>();

            try
            {
                await monitor.RunCheckCycle(cycle.Token);
            }
            finally
            {
                await cycle.CancelAsync();
                await renewal;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // The renewal below abandoned the cycle after losing the lease, and logged why.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check cycle failed; retrying at the next interval");
        }
    }

    // A cycle can outlast the TTL, which would let the lease lapse under a leader that is
    // still probing. Renew at a third of the TTL so two attempts can fail before it expires.
    private async Task RenewLeaseDuringCycle(CancellationTokenSource cycle)
    {
        TimeSpan period = TimeSpan.FromSeconds(Math.Max(1, _options.LeaseTtlSeconds / 3));

        using PeriodicTimer timer = new PeriodicTimer(period);

        try
        {
            while (await timer.WaitForNextTickAsync(cycle.Token))
            {
                if (await StillHoldsLease(cycle.Token))
                    continue;

                _logger.LogWarning(
                    "{Owner} lost the monitor lease mid-cycle; abandoning this cycle so two replicas do not probe at once",
                    Owner);

                SetLeader(false);

                await cycle.CancelAsync();
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // The cycle finished, or the app is shutting down - nothing left to renew.
        }
    }

    // Its own scope, so its own DbContext: the cycle is using the other one concurrently.
    private async Task<bool> StillHoldsLease(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            IMonitorLeaseStore leaseStore = scope.ServiceProvider.GetRequiredService<IMonitorLeaseStore>();

            return await leaseStore.TryAcquireAsync(Owner, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed renewal is not proof the lease is gone; most of the TTL is still left.
            _logger.LogWarning(ex, "Could not renew the monitor lease; retrying before it expires");
            return true;
        }
    }

    // Logged on a change only - a standby pod would otherwise write a line every interval.
    private void SetLeader(bool isLeader)
    {
        if (isLeader == _isLeader)
            return;

        _isLeader = isLeader;

        _logger.LogInformation(isLeader
            ? "{Owner} holds the monitor lease and is now running the checks"
            : "{Owner} does not hold the monitor lease and is standing by", Owner);
    }

    // Shutdown has already cancelled stoppingToken, so this gets its own small budget.
    private async Task ReleaseLease()
    {
        if (_isLeader != true)
            return;

        try
        {
            using CancellationTokenSource timeout = new CancellationTokenSource(ReleaseTimeout);
            using IServiceScope scope = _scopeFactory.CreateScope();

            IMonitorLeaseStore leaseStore = scope.ServiceProvider.GetRequiredService<IMonitorLeaseStore>();

            await leaseStore.ReleaseAsync(Owner, timeout.Token);

            _logger.LogInformation("{Owner} released the monitor lease", Owner);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not release the monitor lease; it will expire on its own");
        }
    }
}
