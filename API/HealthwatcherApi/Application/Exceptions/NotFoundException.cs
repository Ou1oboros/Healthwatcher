using System.Net;

namespace HealthwatcherApi.Application.Exceptions;

public class NotFoundException : AppLayerException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.", HttpStatusCode.NotFound)
    {
    }
}
