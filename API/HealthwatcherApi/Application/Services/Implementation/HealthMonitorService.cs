using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Shared.Common;
using Microsoft.Extensions.Options;

namespace HealthwatcherApi.Application.Services.Implementation;

// Orchestration only: load targets, probe them all, write result + history, commit once.
public class HealthMonitorService : IHealthMonitorService
{
    private readonly ITargetRepository _targetRepository;
    private readonly IHealthProbe _healthProbe;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MonitoringOptions _options;
    private readonly ILogger<HealthMonitorService> _logger;

    public HealthMonitorService(
        ITargetRepository targetRepository,
        IHealthProbe healthProbe,
        IUnitOfWork unitOfWork,
        IOptions<MonitoringOptions> options,
        ILogger<HealthMonitorService> logger)
    {
        _targetRepository = targetRepository;
        _healthProbe = healthProbe;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> RunCheckCycle(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Target> targets = await _targetRepository.GetTrackedEnabledAsync(cancellationToken);
        if (targets.Count == 0)
            return 0;

        HealthCheckRecord[] records = await ProbeAll(targets, cancellationToken);

        // One timestamp for the whole cycle, so every row from this pass lines up on a chart.
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;

        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].RecordCheck(records[i], checkedAt);
            _targetRepository.AddHistory(new TargetHistory(targets[i], records[i].Copy(), checkedAt));
        }

        // One commit per cycle: N targets cost one round trip, not 2N.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Checked {Count} target(s) at {CheckedAt:O}", targets.Count, checkedAt);
        return targets.Count;
    }

    // Concurrent, so a timed-out target doesn't add its delay to the rest.
    private async Task<HealthCheckRecord[]> ProbeAll(IReadOnlyList<Target> targets, CancellationToken cancellationToken)
    {
        using SemaphoreSlim gate = new SemaphoreSlim(_options.MaxConcurrentChecks);

        return await Task.WhenAll(targets.Select(async target =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return await _healthProbe.ProbeAsync(target.Url, cancellationToken);
            }
            finally
            {
                gate.Release();
            }
        }));
    }
}
