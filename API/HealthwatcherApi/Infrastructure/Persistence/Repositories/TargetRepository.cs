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


    public Task<Target?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<Target?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<(IReadOnlyList<Target> Targets, int TotalCount)> GetPageAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
