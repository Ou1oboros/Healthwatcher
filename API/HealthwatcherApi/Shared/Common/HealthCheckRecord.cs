using System.ComponentModel.DataAnnotations;
using Enums;

namespace HealthwatcherApi.Shared.Common;

public class HealthCheckRecord
{
    public double? ResponseTimeMs { get; private set; }
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;
    public int? StatusCode { get; private set; }

    [MaxLength(ValidationConstants.ErrorMaxLength)]
    public string? Error { get; private set; }

    public HealthCheckRecord()
    {

    }

}
