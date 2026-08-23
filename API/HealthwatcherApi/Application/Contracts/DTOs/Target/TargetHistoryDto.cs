namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class TargetHistoryDto
{
    public required long Id { get; init; }
    public required DateTimeOffset? CheckedAt { get; init; }
    public required HealthCheckRecordDto HealthCheckRecord { get; init; }
}
