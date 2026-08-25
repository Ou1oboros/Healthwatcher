using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Domain.Services.Abstraction;

// In Domain so the monitor depends on "probe a URL", not on HttpClient; implemented in Infrastructure.
public interface IHealthProbe
{
    /// <summary>
    /// An unreachable, slow, or invalid target comes back as a Down record rather than throwing.
    /// Only cancellation propagates.
    /// </summary>
    Task<HealthCheckRecord> ProbeAsync(string url, CancellationToken cancellationToken = default);
}
