using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Domain.Entities;

// One immutable row per completed check.
// The composite index below backs the history/uptime queries (filtered + ordered by it).
[Index(nameof(TargetId), nameof(CheckedAt))]
public class TargetHistory
{
    [Key]
    public long Id { get; private set; }
    public Guid TargetId { get; private set; }
    public DateTimeOffset? CheckedAt { get; private set; }
    public HealthCheckRecord HealthCheckRecord { get; private set; } = null!;

    public TargetHistory(Target target, HealthCheckRecord healthCheckRecord, DateTimeOffset checkedAt)
    {
        ArgumentNullException.ThrowIfNull(target);
        TargetId = target.Id;
        CheckedAt = checkedAt;
        HealthCheckRecord = healthCheckRecord ?? throw new ArgumentNullException(nameof(healthCheckRecord));
    }

    private TargetHistory() // EF Core materialisation ctor
    {

    }

}
