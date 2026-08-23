using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Domain.Entities;

/// <summary>
/// One immutable row per completed check. The (target, time) index is what the
/// history and uptime queries are ordered and filtered by.
/// </summary>
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

    /// <summary>Required by EF Core for materialisation. Do not use directly.</summary>
    private TargetHistory()
    {

    }

}
