using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Shared;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Domain.Entities;

public class Target : BaseEntity
{
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; private set; }  = null!;

    [Required, MaxLength(ValidationConstants.TargetUrlMaxLength)]
    public string Url { get; private set; } = null!;

    /// <summary>Null until the monitor has probed this target at least once.</summary>
    public DateTimeOffset? CheckedAt { get; private set; }
    public bool IsEnabled { get; private set; }  = true;
    public HealthCheckRecord HealthCheckRecord { get; private set; } = null!;

    public Target(string name, string url, HealthCheckRecord healthCheckRecord)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        HealthCheckRecord = healthCheckRecord ?? throw new ArgumentNullException(nameof(healthCheckRecord));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessException("Target name cannot be empty.");

        Name = name.Trim();
    }

    /// <summary>
    /// Replaces the latest known result. <paramref name="checkedAt"/> is passed in rather
    /// than read from the clock so the target and its history row share one timestamp.
    /// </summary>
    public void RecordCheck(HealthCheckRecord healthCheckRecord, DateTimeOffset checkedAt)
    {
        HealthCheckRecord = healthCheckRecord ?? throw new ArgumentNullException(nameof(healthCheckRecord));
        CheckedAt = checkedAt;
    }

    /// <summary>Required by EF Core for materialisation. Do not use directly.</summary>
    // ReSharper disable once UnusedMember.Local
    private Target()
    {

    }

}
