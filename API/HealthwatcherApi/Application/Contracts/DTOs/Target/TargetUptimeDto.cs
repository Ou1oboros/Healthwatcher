namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

/// <summary>Uptime over a rolling window, derived from the stored history.</summary>
public class TargetUptimeDto
{
    public required Guid TargetId { get; init; }
    public required int WindowHours { get; init; }
    public required int TotalChecks { get; init; }
    public required int UpChecks { get; init; }

    /// <summary>Zero when the window holds no checks at all — reported alongside TotalChecks so the UI can tell the two apart.</summary>
    public required double UptimePercentage { get; init; }
}
