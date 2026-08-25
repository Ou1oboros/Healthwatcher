namespace HealthwatcherApi.Infrastructure.Monitoring.Leasing;

// The single row every replica competes for: whoever holds it runs the check timer.
// Coordination, not business data, so it deliberately skips BaseEntity.
public class MonitorLease
{
    /// <summary>There is only ever one lease; the migration seeds the row.</summary>
    public const int SingletonId = 1;

    public int Id { get; private set; } = SingletonId;

    /// <summary>Machine name of the holder - the pod name inside Kubernetes.</summary>
    public string Owner { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// The concurrency token, rewritten on every grant. Of two pods that read the same expired
    /// lease, only the first to save matches it; the other throws instead of becoming a second leader.
    /// </summary>
    public Guid Token { get; private set; }

    /// <summary>Renewable by the current holder, claimable by anyone once it has expired.</summary>
    public bool IsFreeFor(string owner, DateTimeOffset now) => Owner == owner || ExpiresAt <= now;

    public void GrantTo(string owner, DateTimeOffset expiresAt)
    {
        Owner = owner;
        ExpiresAt = expiresAt;
        Token = Guid.NewGuid();
    }

    /// <summary>Expires the lease on shutdown, so a standby takes over without waiting out the TTL.</summary>
    public void Release(DateTimeOffset now)
    {
        ExpiresAt = now;
        Token = Guid.NewGuid();
    }
}
