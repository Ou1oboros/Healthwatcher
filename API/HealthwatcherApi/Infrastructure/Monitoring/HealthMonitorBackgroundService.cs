using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Monitoring;

// Just owns the timer. The actual work is in IHealthMonitorService, which is scoped -
// each cycle gets its own DbContext instead of one shared for the process lifetime.
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
    }

    // A bad cycle (dropped DB connection, a buggy probe) must not kill monitoring for the pod.
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
