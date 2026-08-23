using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Domain.Services.Abstraction;

// Lives in Domain so the monitor depends on "probe a URL", not on HttpClient directly.
// Actual implementation is in Infrastructure.
public interface IHealthProbe
{
    /// <summary>
    /// Never throws for an unreachable, slow, or invalid target - those just come back as
    /// a Down record. Only cancellation of the token propagates as an exception.
    /// </summary>
    Task<HealthCheckRecord> ProbeAsync(string url, CancellationToken cancellationToken = default);
}
