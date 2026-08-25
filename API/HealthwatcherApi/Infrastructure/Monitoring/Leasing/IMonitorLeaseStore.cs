namespace HealthwatcherApi.Infrastructure.Monitoring.Leasing;

// Leader election for the check timer, backed by a database row rather than anything
// Kubernetes-specific, so it behaves the same wherever the app runs.
public interface IMonitorLeaseStore
{
    /// <summary>Claims the lease, or renews it if this owner already holds it.</summary>
    Task<bool> TryAcquireAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>Gives the lease up if this owner still holds it.</summary>
    Task ReleaseAsync(string owner, CancellationToken cancellationToken = default);
}
