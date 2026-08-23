namespace HealthwatcherApi.Application.Options;

// Bound from the "Monitoring" config section - backed by a ConfigMap in the cluster,
// so interval/targets change without a rebuild.
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int IntervalSeconds { get; set; } = 30;

    // Exceeding this marks the target Down; there's no retry.
    public int TimeoutSeconds { get; set; } = 10;

    public int MaxConcurrentChecks { get; set; } = 10;

    // Seeded on startup if not already present. Empty is fine.
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
