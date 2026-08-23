namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

/// <summary>One past check, as returned by the per-target history endpoint.</summary>
public class TargetHistoryDto
{
    public required long Id { get; init; }
    public required DateTimeOffset? CheckedAt { get; init; }
    public required HealthCheckRecordDto HealthCheckRecord { get; init; }
}
