using System.Diagnostics;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Infrastructure.Monitoring;

/// <summary>
/// Turns "is this URL healthy?" into a single GET. Every failure mode a target can throw
/// at us — bad DNS, refused connection, TLS failure, timeout, malformed URL — is caught
/// and returned as a Down record, because one broken target must not stop a check cycle.
/// </summary>
public class HttpHealthProbe : IHealthProbe
{
    /// <summary>Named client so its timeout and handler lifetime are configured in one place.</summary>
    public const string HttpClientName = "health-probe";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpHealthProbe> _logger;

    public HttpHealthProbe(IHttpClientFactory httpClientFactory, ILogger<HttpHealthProbe> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckRecord> ProbeAsync(string url, CancellationToken cancellationToken = default)
    {
        HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
        long startedAt = Stopwatch.GetTimestamp();

        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            // ResponseHeadersRead: the status line is the answer, so we never pull the body down.
            using HttpResponseMessage response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            double elapsedMs = Elapsed(startedAt);
            int statusCode = (int)response.StatusCode;

            return response.IsSuccessStatusCode
                ? HealthCheckRecord.Up(statusCode, elapsedMs)
                : HealthCheckRecord.Down(statusCode, elapsedMs, $"HTTP {statusCode} {response.ReasonPhrase}".Trim());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The app is shutting down, not the target failing. Let the cycle unwind.
            throw;
        }
        catch (OperationCanceledException)
        {
            // HttpClient signals its own timeout as a cancellation with no token behind it.
            _logger.LogDebug("Probe of {Url} timed out after {Timeout}", url, client.Timeout);
            return HealthCheckRecord.Down(null, Elapsed(startedAt), $"Timed out after {client.Timeout.TotalSeconds:0.#}s.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Probe of {Url} could not reach the host", url);
            return HealthCheckRecord.Down(null, Elapsed(startedAt), ex.Message);
        }
        catch (Exception ex)
        {
            // Invalid URL, TLS failure, anything else. Still just a Down result.
            _logger.LogWarning(ex, "Probe of {Url} failed unexpectedly", url);
            return HealthCheckRecord.Down(null, Elapsed(startedAt), ex.Message);
        }
    }

    private static double Elapsed(long startedAt) => Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
}
