namespace HealthwatcherApi.Application.Options;

// Bound from the "Monitoring" section, backed by a ConfigMap in the cluster.
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int IntervalSeconds { get; set; } = 30;

    // Exceeding this marks the target Down; there is no retry.
    public int TimeoutSeconds { get; set; } = 10;

    public int MaxConcurrentChecks { get; set; } = 10;

    // Seeded on startup if not already present.
    public string[] Targets { get; set; } = [];

    // How long a claim on the monitor lease stays valid. A leader that dies without releasing
    // it stalls checks for this TTL plus up to one interval, until a standby's next tick.
    public int LeaseTtlSeconds { get; set; } = 90;

    public IEnumerable<string> Validate()
    {
        if (IntervalSeconds < 1)
            yield return $"{nameof(IntervalSeconds)} must be at least 1.";

        if (TimeoutSeconds is < 1 or > 120)
            yield return $"{nameof(TimeoutSeconds)} must be between 1 and 120.";

        if (MaxConcurrentChecks < 1)
            yield return $"{nameof(MaxConcurrentChecks)} must be at least 1.";

        // Twice the interval, so one missed renewal doesn't hand leadership to another pod.
        if (LeaseTtlSeconds < IntervalSeconds * 2)
            yield return $"{nameof(LeaseTtlSeconds)} must be at least twice {nameof(IntervalSeconds)}.";
    }
}
