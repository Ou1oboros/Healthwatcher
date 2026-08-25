using System.Net;
using System.Net.Http.Json;
using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Application.Contracts.DTOs.Target;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Tests.Integration;

// HTTP in, JSON out, through the real pipeline. A factory per test rather than an
// IClassFixture, so writes in one test can't make another depend on run order.
public class TargetEndpointsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new TestWebApplicationFactory();
    private readonly HttpClient _client;

    public TargetEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        HttpResponseMessage response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTargets_ReturnsAnEmptyPage_WhenNoneAreSeeded()
    {
        PagedResult<PreviewTargetDto>? page =
            await _client.GetFromJsonAsync<PagedResult<PreviewTargetDto>>("/api/targets?pageIndex=1&pageSize=20");

        Assert.NotNull(page);
        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task PostTarget_CreatesTheTargetAndReturnsALocationHeader()
    {
        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/targets", new InsertTargetDto { Url = "https://example.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        PreviewTargetDto? created = await response.Content.ReadFromJsonAsync<PreviewTargetDto>();
        Assert.NotNull(created);
        Assert.Equal("example", created.Name);
        Assert.Equal("https://example.com/", created.Url);

        HttpResponseMessage getResponse = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task PostTarget_ReturnsBadRequest_WhenTheUrlIsInvalid()
    {
        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/targets", new InsertTargetDto { Url = "not a url" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task GetTargetById_ReturnsNotFound_WhenTheTargetDoesNotExist()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/targets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTarget_RemovesItFromTheListing()
    {
        HttpResponseMessage createResponse =
            await _client.PostAsJsonAsync("/api/targets", new InsertTargetDto { Url = "https://example.com" });
        PreviewTargetDto created = (await createResponse.Content.ReadFromJsonAsync<PreviewTargetDto>())!;

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/targets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/targets/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
