using HealthwatcherApi.Application.Contracts;

namespace HealthwatcherApi.Tests.Application;

/// <summary>
/// PageRequest is bound straight from the query string, so callers control it.
/// It has to normalise anything they send.
/// </summary>
public class PageRequestTests
{
    [Fact]
    public void Defaults_ToTheFirstPage()
    {
        PageRequest page = new PageRequest();

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(20, page.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PageIndex_BelowOneClampsToOne(int requested)
    {
        Assert.Equal(1, new PageRequest { PageIndex = requested }.PageIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PageRequest.MaxPageSize + 1)]
    [InlineData(50_000)]
    public void PageSize_OutOfRangeFallsBackToTheDefault(int requested)
    {
        Assert.Equal(20, new PageRequest { PageSize = requested }.PageSize);
    }

    [Fact]
    public void PageSize_AtTheLimitIsAccepted()
    {
        Assert.Equal(PageRequest.MaxPageSize, new PageRequest { PageSize = PageRequest.MaxPageSize }.PageSize);
    }
}
