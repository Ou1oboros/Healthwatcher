using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Domain.Entities;

public class Target : BaseEntity
{
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; set; } = null!;


    public Target(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>Required by EF Core for materialisation. Do not use directly.</summary>
    private Target()
    {

    }

}
