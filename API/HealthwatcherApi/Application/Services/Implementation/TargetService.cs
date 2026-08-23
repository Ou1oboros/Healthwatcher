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
        Target? target = await _targetRepository.GetByIdAsync(id, cancellationToken);
        if (target == null)
            throw new ApplicationException("Target not found");

        return target.ToDto();

    }

    public Task<PagedResult<PreviewTargetDto>> GetTargets(PageRequest page, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public async Task<PreviewTargetDto> InsertTarget(InsertTargetDto insertTargetDto,
        CancellationToken cancellationToken = default)
    {
        Target? target = await _targetDomainService.InsertTarget(insertTargetDto.Url);
        if (target != null) return target.ToPreviewDto();
        else throw new ApplicationException("Failed to insert target");
    }

    public Task RenameTarget(Guid targetId, RenameTargetDto renameTargetDto, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task DeleteTarget(Guid targetId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
