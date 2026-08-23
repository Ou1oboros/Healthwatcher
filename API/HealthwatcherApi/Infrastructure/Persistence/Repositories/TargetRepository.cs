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
        throw new NotImplementedException();

    public Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public async Task<Target?> InsertTargetAsync(string name, string url, CancellationToken cancellationToken = default)
    {
        EntityEntry<Target> newTarget =
            await _context.Targets.AddAsync(new Target(name, url, new HealthCheckRecord()), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return newTarget.Entity;
    }

}
