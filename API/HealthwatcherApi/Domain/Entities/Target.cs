using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Domain.Entities;

public class Target : BaseEntity
{
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; private set; }  = null!;
    [Required, MaxLength(ValidationConstants.TargetUrlMaxLength)]
    public string? Url { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }
    public bool IsEnabled { get; private set; }  = true;
    public HealthCheckRecord HealthCheckRecord { get; private set; } = null!;

    public Target(string name, string url, HealthCheckRecord healthCheckRecord)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Url = url ?? throw new ArgumentNullException(nameof(url));
        CheckedAt = DateTimeOffset.UtcNow;
        HealthCheckRecord = healthCheckRecord ?? throw new ArgumentNullException(nameof(healthCheckRecord));


    }

    /// <summary>Required by EF Core for materialisation. Do not use directly.</summary>
    // ReSharper disable once UnusedMember.Local
    private Target()
    {

    }

}
