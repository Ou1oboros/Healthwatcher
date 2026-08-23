using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

    public async Task<Target?> InsertTargetAsync(string name, string url, CancellationToken cancellationToken = default)
    {
        EntityEntry<Target> newTarget =
            await _context.Targets.AddAsync(new Target(name, url, new HealthCheckRecord()), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return newTarget.Entity;
    }

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

}
