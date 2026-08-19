using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Application.Contracts;

public class InsertTargetDto
{
    [Required]
    [MaxLength(ValidationConstants.TargetNameMaxLength)]
    public string Name { get; set; } = null!;
}
