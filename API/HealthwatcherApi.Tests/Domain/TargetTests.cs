using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Tests.Domain;

/// <summary>
/// Entity behaviour: no mocks, no database, no DI. If a rule can be tested here,
/// test it here — these are the fastest tests you will write.
/// </summary>
public class TargetTests
{
    [Fact]
    public void Constructor_RejectsANullName()
    {
        Assert.Throws<ArgumentNullException>(() => new Target(null!, "https://example.com", new HealthCheckRecord()));
    }

    [Fact]
    public void Constructor_RejectsANullUrl()
    {
        Assert.Throws<ArgumentNullException>(() => new Target("example", null!, new HealthCheckRecord()));
    }

    [Fact]
    public void Constructor_RejectsANullHealthCheckRecord()
    {
        Assert.Throws<ArgumentNullException>(() => new Target("example", "https://example.com", null!));
    }

    [Fact]
    public void Constructor_IsEnabledByDefault()
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());

        Assert.True(target.IsEnabled);
        Assert.Null(target.CheckedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_RejectsAnEmptyName(string? newName)
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());

        Assert.Throws<BusinessException>(() => target.Rename(newName!));
    }

    [Fact]
    public void Rename_TrimsWhitespace()
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());

        target.Rename("  My Service  ");

        Assert.Equal("My Service", target.Name);
    }

    [Fact]
    public void RecordCheck_UpdatesTheHealthCheckRecordAndCheckedAt()
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());
        HealthCheckRecord record = HealthCheckRecord.Up(200, 42.5);
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;

        target.RecordCheck(record, checkedAt);

        Assert.Same(record, target.HealthCheckRecord);
        Assert.Equal(checkedAt, target.CheckedAt);
    }

    [Fact]
    public void RecordCheck_RejectsANullRecord()
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());

        Assert.Throws<ArgumentNullException>(() => target.RecordCheck(null!, DateTimeOffset.UtcNow));
    }
}
