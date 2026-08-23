using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.IRepositories;

public interface ITargetRepository
{
    /// <summary>Read-only, for queries that do not lead to a write.</summary>
    Task<Target?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, for mutation followed by a unit-of-work commit.</summary>
    Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Target?> InsertTargetAsync(string name, string url, CancellationToken cancellationToken = default);
}
