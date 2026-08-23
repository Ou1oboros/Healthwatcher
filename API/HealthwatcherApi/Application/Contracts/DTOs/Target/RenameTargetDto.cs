using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class RenameTargetDto
{
    [Required]
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; set; } = null!;
}
