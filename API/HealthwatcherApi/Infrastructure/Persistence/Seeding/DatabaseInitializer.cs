using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Persistence.Seeding;

// Migrates + seeds the configured targets before anything serves traffic. Registered ahead
// of the monitor, so a fresh pod with an empty volume comes up with no manual migration step.
public class DatabaseInitializer : IHostedService
{
    private static readonly TimeSpan LockPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MonitoringOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IServiceScopeFactory scopeFactory,
        IOptions<MonitoringOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Replicas all start at once against the same file; migrating and seeding are not
        // safe to run concurrently.
        await using FileStream? startupLock = await AcquireStartupLock(context, cancellationToken);

        await context.Database.MigrateAsync(cancellationToken);

        await EnableWriteAheadLogging(context, cancellationToken);

        await SeedTargets(
            scope.ServiceProvider.GetRequiredService<ITargetDomainService>(), context, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // An exclusive handle on a lock file next to the database - an flock underneath, so it
    // holds across pods sharing the volume. Timing out throws, and Kubernetes retries the pod.
    private async Task<FileStream?> AcquireStartupLock(AppDbContext context, CancellationToken cancellationToken)
    {
        string dataSource = context.Database.GetDbConnection().DataSource;

        // An in-memory database (the test host) is private to this process.
        if (string.IsNullOrEmpty(dataSource) || dataSource.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            return null;

        DateTimeOffset giveUpAt = DateTimeOffset.UtcNow + LockTimeout;
        bool announcedWait = false;

        while (true)
        {
            try
            {
                return new FileStream($"{dataSource}.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (DateTimeOffset.UtcNow < giveUpAt)
            {
                // Once, not on every poll.
                if (!announcedWait)
                {
                    _logger.LogInformation(
                        "Another replica is initialising the database; waiting for the startup lock");
                    announcedWait = true;
                }

                await Task.Delay(LockPollInterval, cancellationToken);
            }
        }
    }

    // Without WAL the replica writing check results and those reading for API requests block
    // each other. Stored in the file, so this is a no-op after the first run.
    private static Task EnableWriteAheadLogging(AppDbContext context, CancellationToken cancellationToken)
        => context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

    // Through the domain service, so a configured URL is normalised and validated exactly
    // like one added from the UI.
    private async Task SeedTargets(
        ITargetDomainService targetDomainService, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        foreach (string url in _options.Targets.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            try
            {
                await targetDomainService.InsertTarget(url, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Seeded monitored target {Url}", url);
            }
            catch (BusinessException ex)
            {
                // Already seeded, or a typo in the ConfigMap - neither is worth refusing to boot over.
                _logger.LogDebug("Skipped seeding {Url}: {Reason}", url, ex.Message);
            }
        }
    }
}
