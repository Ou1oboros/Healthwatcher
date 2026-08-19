using System.Net;

namespace HealthwatcherApi.Application.Exceptions;

/// <summary>
/// Base for expected application failures that map onto an HTTP status code.
/// Anything not derived from this is treated as unexpected and reported as a 500.
/// </summary>
public abstract class AppLayerException : Exception
{
    public HttpStatusCode StatusCode { get; }

    protected AppLayerException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    protected AppLayerException(string message, HttpStatusCode statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
