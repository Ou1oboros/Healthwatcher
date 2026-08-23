using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Domain.Services.Abstraction;

/// <summary>
/// Performs one check against one URL. Declared here so the monitor depends on the
/// idea of "probe a URL" rather than on HttpClient; the implementation is in Infrastructure.
/// </summary>
public interface IHealthProbe
{
    /// <summary>
    /// Never throws for an unreachable, slow or invalid target — those come back as a
    /// Down record. Only cancellation of <paramref name="cancellationToken"/> propagates.
    /// </summary>
    Task<HealthCheckRecord> ProbeAsync(string url, CancellationToken cancellationToken = default);
}
