using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Monitoring.Leasing;

// One read, one conditional update, and a caught concurrency exception. No locks are held
// between calls, so a pod that dies mid-lease costs one TTL rather than blocking the others.
public class MonitorLeaseStore : IMonitorLeaseStore
{
    private readonly AppDbContext _context;
    private readonly MonitoringOptions _options;
    private readonly ILogger<MonitorLeaseStore> _logger;

    public MonitorLeaseStore(
        AppDbContext context,
        IOptions<MonitoringOptions> options,
        ILogger<MonitorLeaseStore> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(string owner, CancellationToken cancellationToken = default)
    {
        MonitorLease? lease = await LoadLease(cancellationToken);
        if (lease is null)
            return false;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!lease.IsFreeFor(owner, now))
            return false;

        lease.GrantTo(owner, now.AddSeconds(_options.LeaseTtlSeconds));

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another replica claimed the same expired lease first; nothing to recover from.
            _logger.LogDebug("{Owner} lost the race for the monitor lease", owner);
            return false;
        }
    }

    public async Task ReleaseAsync(string owner, CancellationToken cancellationToken = default)
    {
        MonitorLease? lease = await LoadLease(cancellationToken);
        if (lease is null || lease.Owner != owner)
            return;

        lease.Release(DateTimeOffset.UtcNow);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else already took over - nothing left to give up.
        }
    }

    private async Task<MonitorLease?> LoadLease(CancellationToken cancellationToken)
    {
        MonitorLease? lease = await _context.MonitorLeases
            .FirstOrDefaultAsync(l => l.Id == MonitorLease.SingletonId, cancellationToken);

        // Fail closed if the seeded row is missing: every replica leading is worse than none.
        if (lease is null)
            _logger.LogError("The monitor lease row is missing; no replica can run health checks");

        return lease;
    }
}
