using HealthwatcherApi.Application.Contracts;

namespace HealthwatcherApi.Application.Services.Abstraction;

public interface ITargetService
{
    Task<TargetDto> GetTargetById(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PreviewTargetDto>> GetTargets(PageRequest page, CancellationToken cancellationToken = default);

    Task<PreviewTargetDto> InsertTarget(InsertTargetDto insertTargetDto, CancellationToken cancellationToken = default);

    Task RenameTarget(Guid targetId, RenameTargetDto renameTargetDto, CancellationToken cancellationToken = default);

    Task DeleteTarget(Guid targetId, CancellationToken cancellationToken = default);

}
