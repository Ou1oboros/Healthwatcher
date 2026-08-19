using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Infrastructure.Middleware;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        requestContext.CurrentUsername = context.User.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name
            : "anonymous";

        await _next(context);
    }
}

