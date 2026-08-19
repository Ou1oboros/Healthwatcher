using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Domain.Entities;

public class TargetHistory : BaseEntity
{
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; set; } = null!;

    public Target Target { get; private set; } = null!;

    public Guid TargetId { get; private set; }

    public TargetHistory(string name, Target target)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        TargetId = target.Id;
    }

    /// <summary>Required by EF Core for materialisation. Do not use directly.</summary>
    private TargetHistory()
    {

    }

}
