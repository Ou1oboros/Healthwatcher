using HealthwatcherApi.Infrastructure.Middleware;

namespace HealthwatcherApi.Presentation.Config;

public static class MiddlewareConfig
{
    // must be registered first to wrap everything downstream
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionMiddleware>();

    /// <summary>Register after authentication, so there is an identity to record.</summary>
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestContextMiddleware>();
}
