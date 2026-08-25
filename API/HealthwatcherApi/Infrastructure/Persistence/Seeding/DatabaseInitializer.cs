using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.Services.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Infrastructure.Persistence.Seeding;

// Migrates + seeds the configured targets before anything serves traffic. Registered
// ahead of the monitor so the first check cycle finds a migrated schema - this is what
// lets a fresh pod with an empty volume come up working with no manual migration step.
public class DatabaseInitializer : IHostedService
{
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
        await context.Database.MigrateAsync(cancellationToken);

        await SeedTargets(
            scope.ServiceProvider.GetRequiredService<ITargetDomainService>(), context, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Goes through the domain service (not straight into the table) so a configured URL
    // gets normalised/validated the same way one added from the UI would.
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
                // Already seeded on an earlier start, or a typo in the ConfigMap. Neither is
                // worth refusing to boot over — the rest of the list still gets seeded.
                _logger.LogDebug("Skipped seeding {Url}: {Reason}", url, ex.Message);
            }
        }
    }
}
