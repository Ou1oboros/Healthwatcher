using System.Net;
using System.Text.Json;
using HealthwatcherApi.Application.Exceptions;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Infrastructure.Middleware;

// Outermost middleware - register first so it also covers auth, CORS, etc.
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions _serializerOptions =
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppLayerException ex)
        {
            _logger.LogWarning(ex, "Application layer exception");
            await WriteAsync(context, ex.StatusCode, ex.Message);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated");
            await WriteAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request cancelled by the client");
        }
        catch (Exception ex)
        {
            // Never echo ex.Message: it can carry connection strings, SQL or file paths.
            // The traceId ties the response to the log.
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        ErrorResponse response = new ErrorResponse
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            TraceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _serializerOptions));
    }
}
