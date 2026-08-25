using Enums;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Infrastructure.Persistence.Repositories;

public class TargetRepository : ITargetRepository
{
    private readonly AppDbContext _context;

    public TargetRepository(AppDbContext context)
    {
        _context = context;
    }


    public Task<Target?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Targets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Targets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void AddTarget(Target target) => _context.Targets.Add(target);

    public async Task<(IReadOnlyList<Target> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<Target> query = _context.Targets.AsNoTracking().OrderBy(t => t.Name);

        int totalCount = await query.CountAsync(cancellationToken);
        List<Target> items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsByUrlAsync(string url, CancellationToken cancellationToken = default) =>
        _context.Targets.AsNoTracking().AnyAsync(t => t.Url == url, cancellationToken);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Targets.AsNoTracking().AnyAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Target>> GetTrackedEnabledAsync(CancellationToken cancellationToken = default) =>
        await _context.Targets.Where(t => t.IsEnabled).ToListAsync(cancellationToken);

    public void AddHistory(TargetHistory history) => _context.TargetHistory.Add(history);

    public async Task<(IReadOnlyList<TargetHistory> Items, int TotalCount)> GetHistoryPagedAsync(
        Guid targetId, DateTimeOffset? since, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<TargetHistory> query = _context.TargetHistory
            .AsNoTracking()
            .Where(h => h.TargetId == targetId);

        if (since.HasValue)
            query = query.Where(h => h.CheckedAt >= since.Value);

        int totalCount = await query.CountAsync(cancellationToken);
        List<TargetHistory> items = await query
            .OrderByDescending(h => h.CheckedAt)
            .ThenByDescending(h => h.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(int UpCount, int TotalCount)> GetCheckCountsAsync(
        Guid targetId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        // Counted in the database, not by loading a day's worth of rows for two numbers.
        // GroupBy(h => 1) collapses everything into one group, so it is a single SELECT.
        // SUM over a CASE, because EF cannot translate Count(predicate) reaching into an
        // owned type from inside a group aggregate.
        var counts = await _context.TargetHistory
            .AsNoTracking()
            .Where(h => h.TargetId == targetId && h.CheckedAt >= since)
            .GroupBy(h => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Up = g.Sum(h => h.HealthCheckRecord.Status == ConnectionStatus.Up ? 1 : 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // An empty window produces no group, hence no row.
        return counts is null ? (0, 0) : (counts.Up, counts.Total);
    }

}
