using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Application.Contracts.DTOs.Target;

namespace HealthwatcherApi.Application.Services.Abstraction;

public interface ITargetService
{
    Task<TargetDto> GetTargetById(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PreviewTargetDto>> GetTargets(PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Past checks for one target, newest first, optionally limited to a recent window.</summary>
    Task<PagedResult<TargetHistoryDto>> GetTargetHistory(
        Guid targetId, PageRequest page, int? withinHours, CancellationToken cancellationToken = default);

    Task<TargetUptimeDto> GetTargetUptime(Guid targetId, int windowHours, CancellationToken cancellationToken = default);

    Task<PreviewTargetDto> InsertTarget(InsertTargetDto insertTargetDto, CancellationToken cancellationToken = default);

    Task RenameTarget(Guid targetId, RenameTargetDto renameTargetDto, CancellationToken cancellationToken = default);

    Task DeleteTarget(Guid targetId, CancellationToken cancellationToken = default);

}
