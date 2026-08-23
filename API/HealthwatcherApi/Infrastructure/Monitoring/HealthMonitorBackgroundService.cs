using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Monitoring;

/// <summary>
/// Schedules the check cycle. It owns the timer and nothing else — the work itself lives
/// in <see cref="IHealthMonitorService"/>, which is scoped, so each cycle gets its own
/// DbContext rather than sharing one for the lifetime of the process.
/// </summary>
public class HealthMonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<HealthMonitorBackgroundService> _logger;

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
        _logger.LogInformation("Health monitor starting, checking every {Interval}", interval);

        using PeriodicTimer timer = new PeriodicTimer(interval);

        try
        {
            // Run immediately rather than idling through the first interval, so a freshly
            // deployed pod has real data before the dashboard's first poll.
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
    }

    /// <summary>
    /// The loop must outlive any single bad cycle — a dropped database connection or a
    /// bug in one probe cannot be allowed to silently end monitoring for the whole pod.
    /// </summary>
    private async Task RunCycleSafely(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IHealthMonitorService monitor = scope.ServiceProvider.GetRequiredService<IHealthMonitorService>();

            await monitor.RunCheckCycle(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check cycle failed; retrying at the next interval");
        }
    }
}
