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
    private const int MinWindowHours = 1;
    private const int MaxWindowHours = 24 * 7;

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

    public async Task<PagedResult<TargetHistoryDto>> GetTargetHistory(
        Guid targetId, PageRequest page, int? withinHours, CancellationToken cancellationToken = default)
    {
        await EnsureTargetExists(targetId, cancellationToken);

        DateTimeOffset? since = withinHours.HasValue
            ? DateTimeOffset.UtcNow.AddHours(-ClampWindow(withinHours.Value))
            : null;

        (IReadOnlyList<TargetHistory> items, int totalCount) = await _targetRepository.GetHistoryPagedAsync(
            targetId, since, page.PageIndex, page.PageSize, cancellationToken);

        return new PagedResult<TargetHistoryDto>(items.ToDtos(), totalCount, page.PageIndex, page.PageSize);
    }

    public async Task<TargetUptimeDto> GetTargetUptime(
        Guid targetId, int windowHours, CancellationToken cancellationToken = default)
    {
        await EnsureTargetExists(targetId, cancellationToken);

        int window = ClampWindow(windowHours);
        (int upCount, int totalCount) = await _targetRepository.GetCheckCountsAsync(
            targetId, DateTimeOffset.UtcNow.AddHours(-window), cancellationToken);

        return new TargetUptimeDto
        {
            TargetId = targetId,
            WindowHours = window,
            TotalChecks = totalCount,
            UpChecks = upCount,
            // A window with no checks reports 0%, not a divide-by-zero; TotalChecks tells the caller which it is.
            UptimePercentage = totalCount == 0 ? 0 : Math.Round(upCount * 100d / totalCount, 2),
        };
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

    /// <summary>History for an unknown target is a 404, not an empty page.</summary>
    private async Task EnsureTargetExists(Guid targetId, CancellationToken cancellationToken)
    {
        if (!await _targetRepository.ExistsByIdAsync(targetId, cancellationToken))
            throw new NotFoundException(nameof(Target), targetId);
    }

    /// <summary>Keeps a caller from asking for an unbounded scan of the history table.</summary>
    private static int ClampWindow(int hours) => Math.Clamp(hours, MinWindowHours, MaxWindowHours);
}
