namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class TargetDto : BaseDto
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required DateTimeOffset? CheckedAt { get; init; }
    public required bool IsEnabled { get; init; }
    public required HealthCheckRecordDto HealthCheckRecord { get; init; }
}
