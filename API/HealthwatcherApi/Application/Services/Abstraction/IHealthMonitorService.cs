namespace HealthwatcherApi.Application.Services.Abstraction;

// One pass over every enabled target, separate from the hosted service that schedules it,
// so a cycle can be run without a timer.
public interface IHealthMonitorService
{
    /// <returns>How many targets were checked.</returns>
    Task<int> RunCheckCycle(CancellationToken cancellationToken = default);
}
