using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Tests.Domain;

/// <summary>
/// Entity behaviour: no mocks, no database, no DI. If a rule can be tested here,
/// test it here — these are the fastest tests you will write.
/// </summary>
public class TargetTests
{
    [Fact]
    public void Constructor_RejectsMissingName()
    {
        Assert.Throws<ArgumentNullException>(() => new Target(null!));
    }
}
