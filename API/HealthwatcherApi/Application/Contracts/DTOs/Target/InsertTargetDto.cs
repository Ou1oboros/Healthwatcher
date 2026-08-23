using System.ComponentModel.DataAnnotations;
using HealthwatcherApi.Shared;

namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class InsertTargetDto
{
    [Required]
    [MaxLength(ValidationConstants.TargetUrlMaxLength)]
    public string Url { get; set; } = null!;
}
