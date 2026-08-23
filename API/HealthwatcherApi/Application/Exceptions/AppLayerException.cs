using System.Net;

namespace HealthwatcherApi.Application.Exceptions;

// Expected failures that map to a status code. Anything else bubbles up as a 500.
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
