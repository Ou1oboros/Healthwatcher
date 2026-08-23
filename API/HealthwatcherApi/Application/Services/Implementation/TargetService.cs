using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Application.Contracts.DTOs.Target;
using HealthwatcherApi.Application.Exceptions;
using HealthwatcherApi.Application.Mappings;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;

namespace HealthwatcherApi.Application.Services.Implementation;

/// <summary>
/// Orchestration only: load, delegate the rules to the domain, save once, map out.
/// Business rules belong in the entities or <see cref="ITargetDomainService"/>.
/// </summary>
public class TargetService : ITargetService
{
    private readonly ITargetRepository _targetRepository;
    private readonly ITargetDomainService _targetDomainService;
    private readonly IUnitOfWork _unitOfWork;

    public TargetService(
        ITargetRepository targetRepository,
        ITargetDomainService targetDomainService,
        IUnitOfWork unitOfWork)
    {
        _targetRepository = targetRepository;
        _targetDomainService = targetDomainService;
        _unitOfWork = unitOfWork;
    }


    public async Task<TargetDto> GetTargetById(Guid id, CancellationToken cancellationToken = default)
    {
        Target target = await _targetRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Target), id);

        return target.ToDto();
    }

    public async Task<PagedResult<PreviewTargetDto>> GetTargets(PageRequest page, CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<Target> items, int totalCount) =
            await _targetRepository.GetPagedAsync(page.PageIndex, page.PageSize, cancellationToken);

        return new PagedResult<PreviewTargetDto>(items.ToPreviewDtos(), totalCount, page.PageIndex, page.PageSize);
    }

    public async Task<PreviewTargetDto> InsertTarget(InsertTargetDto insertTargetDto,
        CancellationToken cancellationToken = default)
    {
        Target target = await _targetDomainService.InsertTarget(insertTargetDto.Url, cancellationToken);
        return target.ToPreviewDto();
    }

    public async Task RenameTarget(Guid targetId, RenameTargetDto renameTargetDto, CancellationToken cancellationToken = default)
    {
        Target target = await _targetRepository.GetTrackedByIdAsync(targetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Target), targetId);

        _targetDomainService.RenameTarget(target, renameTargetDto.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTarget(Guid targetId, CancellationToken cancellationToken = default)
    {
        Target target = await _targetRepository.GetTrackedByIdAsync(targetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Target), targetId);

        _targetDomainService.DeleteTarget(target);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
