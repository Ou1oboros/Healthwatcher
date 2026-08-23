namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class TargetUptimeDto
{
    public required Guid TargetId { get; init; }
    public required int WindowHours { get; init; }
    public required int TotalChecks { get; init; }
    public required int UpChecks { get; init; }

    // 0 when TotalChecks is also 0 - not "0% uptime", just no data yet.
    public required double UptimePercentage { get; init; }
}
