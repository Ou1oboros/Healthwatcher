using System.Net;

namespace HealthwatcherApi.Application.Exceptions;

public class BadRequestException : AppLayerException
{
    public BadRequestException(string message)
        : base(message, HttpStatusCode.BadRequest)
    {
    }
}
