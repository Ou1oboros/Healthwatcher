using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.IRepositories;

public interface ITargetRepository
{
    Task<Target?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Tracked, for callers that mutate and save through the unit of work.
    Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void AddTarget(Target target);

    Task<(IReadOnlyList<Target> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Tracked, since the monitor writes results back after probing.
    Task<IReadOnlyList<Target>> GetTrackedEnabledAsync(CancellationToken cancellationToken = default);

    // Queues the row; nothing is persisted until the next commit.
    void AddHistory(TargetHistory history);

    // Newest first; a null since means no lower bound.
    Task<(IReadOnlyList<TargetHistory> Items, int TotalCount)> GetHistoryPagedAsync(
        Guid targetId, DateTimeOffset? since, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    Task<(int UpCount, int TotalCount)> GetCheckCountsAsync(
        Guid targetId, DateTimeOffset since, CancellationToken cancellationToken = default);
}
