using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.IRepositories;

public interface ITargetRepository
{
    /// <summary>Read-only, for queries that do not lead to a write.</summary>
    Task<Target?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tracked, for mutation followed by a unit-of-work commit.</summary>
    Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Target?> InsertTargetAsync(string name, string url, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Target> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Used to enforce that a target's URL is unique before insert.</summary>
    Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Every target the monitor should probe, tracked so results can be written back.</summary>
    Task<IReadOnlyList<Target>> GetTrackedEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>Queues a history row; it is persisted by the next unit-of-work commit.</summary>
    void AddHistory(TargetHistory history);

    /// <summary>Newest first. <paramref name="since"/> is ignored when null.</summary>
    Task<(IReadOnlyList<TargetHistory> Items, int TotalCount)> GetHistoryPagedAsync(
        Guid targetId, DateTimeOffset? since, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Check counts since a point in time, for the uptime percentage.</summary>
    Task<(int UpCount, int TotalCount)> GetCheckCountsAsync(
        Guid targetId, DateTimeOffset since, CancellationToken cancellationToken = default);
}
