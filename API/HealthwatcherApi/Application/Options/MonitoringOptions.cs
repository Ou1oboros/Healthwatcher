namespace HealthwatcherApi.Application.Options;

/// <summary>
/// Bound from the "Monitoring" configuration section. In the cluster this comes from a
/// ConfigMap, so the interval and the monitored URL list are changed without a rebuild.
/// </summary>
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>How long to wait between the start of one check cycle and the next.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Per-request budget. A target that exceeds it is recorded as Down, not retried.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Upper bound on probes in flight at once, so a large target list cannot exhaust sockets.</summary>
    public int MaxConcurrentChecks { get; set; } = 10;

    /// <summary>Seeded into the database on startup if not already present. Empty is valid.</summary>
    public string[] Targets { get; set; } = [];

    public IEnumerable<string> Validate()
    {
        if (IntervalSeconds < 1)
            yield return $"{nameof(IntervalSeconds)} must be at least 1.";

        if (TimeoutSeconds is < 1 or > 120)
            yield return $"{nameof(TimeoutSeconds)} must be between 1 and 120.";

        if (MaxConcurrentChecks < 1)
            yield return $"{nameof(MaxConcurrentChecks)} must be at least 1.";
    }
}
