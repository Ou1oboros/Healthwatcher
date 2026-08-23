using Enums;

namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class HealthCheckRecordDto
{
    public double? ResponseTimeMs { get; init; }
    public required ConnectionStatus Status { get; init; }
    public int? StatusCode { get; init; }
    public string? Error { get; init; }
}
