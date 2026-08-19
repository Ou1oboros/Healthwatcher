namespace HealthwatcherApi.Shared.Common;

public class ErrorResponse
{
    public required int StatusCode { get; init; }
    public required string Message { get; init; }

    /// <summary>Correlates the response with the server log entry for the same request.</summary>
    public string? TraceId { get; init; }
}
