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

    public DateTimeOffset? CheckedAt { get; private set; } // null until the first probe
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

    // checkedAt is passed in so the target and its history row share the exact timestamp.
    public void RecordCheck(HealthCheckRecord healthCheckRecord, DateTimeOffset checkedAt)
    {
        HealthCheckRecord = healthCheckRecord ?? throw new ArgumentNullException(nameof(healthCheckRecord));
        CheckedAt = checkedAt;
    }

    // ReSharper disable once UnusedMember.Local
    private Target() // EF Core materialisation ctor
    {

    }

}
