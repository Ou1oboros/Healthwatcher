using HealthwatcherApi.Infrastructure.Middleware;

namespace HealthwatcherApi.Presentation.Config;

public static class MiddlewareConfig
{
    /// <summary>Register first — it must wrap everything downstream to catch it.</summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionMiddleware>();

    /// <summary>Register after authentication, so there is an identity to record.</summary>
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestContextMiddleware>();
}
