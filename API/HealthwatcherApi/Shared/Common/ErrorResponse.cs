namespace HealthwatcherApi.Shared.Common;

public class ErrorResponse
{
    public required int StatusCode { get; init; }
    public required string Message { get; init; }

    public string? TraceId { get; init; }
}
