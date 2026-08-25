using System.ComponentModel.DataAnnotations;
using Enums;

namespace HealthwatcherApi.Shared.Common;

// Outcome of a single probe: a Target holds its latest, a TargetHistory row holds a past one.
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

    public static HealthCheckRecord Up(int statusCode, double responseTimeMs) => new HealthCheckRecord
    {
        Status = ConnectionStatus.Up,
        StatusCode = statusCode,
        ResponseTimeMs = responseTimeMs,
    };

    /// <summary>A failed check; <paramref name="statusCode"/> is null when the host was never reached.</summary>
    public static HealthCheckRecord Down(int? statusCode, double? responseTimeMs, string error) => new HealthCheckRecord
    {
        Status = ConnectionStatus.Down,
        StatusCode = statusCode,
        ResponseTimeMs = responseTimeMs,
        Error = Truncate(error),
    };

    /// <summary>EF Core will not let one owned instance belong to two owners, hence the copy.</summary>
    public HealthCheckRecord Copy() => new HealthCheckRecord
    {
        Status = Status,
        StatusCode = StatusCode,
        ResponseTimeMs = ResponseTimeMs,
        Error = Error,
    };

    // Exception messages are unbounded; the column is not.
    private static string Truncate(string error) =>
        error.Length <= ValidationConstants.ErrorMaxLength ? error : error[..ValidationConstants.ErrorMaxLength];
}
