namespace HealthwatcherApi.Application.Services.Abstraction;

/// <summary>
/// One pass over every enabled target. Kept separate from the hosted service that
/// schedules it so a cycle can be run — and tested — without waiting for a timer.
/// </summary>
public interface IHealthMonitorService
{
    /// <returns>How many targets were checked.</returns>
    Task<int> RunCheckCycle(CancellationToken cancellationToken = default);
}
