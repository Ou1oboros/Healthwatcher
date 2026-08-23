using System.ComponentModel.DataAnnotations;
using Enums;

namespace HealthwatcherApi.Shared.Common;

/// <summary>
/// The outcome of a single probe. Owned by whoever holds it (a <c>Target</c> holds its
/// latest, a <c>TargetHistory</c> row holds one past result), so it is written through
/// the factories below and never mutated afterwards.
/// </summary>
public class HealthCheckRecord
{
    public double? ResponseTimeMs { get; private set; }
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;
    public int? StatusCode { get; private set; }

    [MaxLength(ValidationConstants.ErrorMaxLength)]
    public string? Error { get; private set; }

    /// <summary>A target that has never been probed yet: Unknown, with nothing to report.</summary>
    public HealthCheckRecord()
    {

    }

    public static HealthCheckRecord Up(int statusCode, double responseTimeMs) => new HealthCheckRecord
    {
        Status = ConnectionStatus.Up,
        StatusCode = statusCode,
        ResponseTimeMs = responseTimeMs,
    };

    /// <summary>
    /// A failed check. <paramref name="statusCode"/> is null when the host was never
    /// reached at all (DNS failure, timeout, refused connection).
    /// </summary>
    public static HealthCheckRecord Down(int? statusCode, double? responseTimeMs, string error) => new HealthCheckRecord
    {
        Status = ConnectionStatus.Down,
        StatusCode = statusCode,
        ResponseTimeMs = responseTimeMs,
        Error = Truncate(error),
    };

    /// <summary>
    /// EF Core will not let one owned instance belong to two owners, so the monitor
    /// takes a copy for the history row rather than reusing the target's record.
    /// </summary>
    public HealthCheckRecord Copy() => new HealthCheckRecord
    {
        Status = Status,
        StatusCode = StatusCode,
        ResponseTimeMs = ResponseTimeMs,
        Error = Error,
    };

    /// <summary>Exception messages are unbounded; the column is not.</summary>
    private static string Truncate(string error) =>
        error.Length <= ValidationConstants.ErrorMaxLength ? error : error[..ValidationConstants.ErrorMaxLength];
}
