namespace HealthwatcherApi.Application.Services.Abstraction;

// One pass over every enabled target. Separate from the hosted service that schedules it
// so a cycle can be run (and tested) without waiting on a timer.
public interface IHealthMonitorService
{
    /// <returns>How many targets were checked.</returns>
    Task<int> RunCheckCycle(CancellationToken cancellationToken = default);
}
