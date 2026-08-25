using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Implementation;
using HealthwatcherApi.Shared.Common;
using NSubstitute;

namespace HealthwatcherApi.Tests.Domain;

// Rules that span more than one entity. InsertTarget needs the repository, so it is
// substituted; RenameTarget and DeleteTarget act on the entity directly.
public class TargetDomainServiceTests
{
    private readonly ITargetRepository _targetRepository = Substitute.For<ITargetRepository>();
    private readonly TargetDomainService _sut;

    public TargetDomainServiceTests()
    {
        _sut = new TargetDomainService(_targetRepository);
    }

    [Fact]
    public async Task InsertTarget_NormalizesABareHostToHttps()
    {
        Target target = await _sut.InsertTarget("github.com");

        Assert.Equal("https://github.com/", target.Url);
    }

    [Theory]
    [InlineData("https://google.com", "google")]
    [InlineData("https://www.google.com", "google")]
    [InlineData("https://api.github.com", "github")]
    [InlineData("https://www.bbc.co.uk", "bbc")]
    [InlineData("https://nic.gov.sa", "nic")]
    [InlineData("https://localhost:8080", "localhost")]
    [InlineData("https://192.168.1.10", "192.168.1.10")]
    public async Task InsertTarget_ExtractsANameFromTheHost(string url, string expectedName)
    {
        Target target = await _sut.InsertTarget(url);

        Assert.Equal(expectedName, target.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("https://")]
    public async Task InsertTarget_RejectsAnInvalidUrl(string url)
    {
        await Assert.ThrowsAsync<BusinessException>(() => _sut.InsertTarget(url));
    }

    [Fact]
    public async Task InsertTarget_RejectsADuplicateUrl()
    {
        _targetRepository.ExistsByUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<BusinessException>(() => _sut.InsertTarget("https://github.com"));
    }

    // Queued, not persisted: committing is the caller's job.
    [Fact]
    public async Task InsertTarget_QueuesTheNewTargetOnTheRepository()
    {
        Target target = await _sut.InsertTarget("github.com");

        _targetRepository.Received(1).AddTarget(target);
    }

    [Fact]
    public async Task InsertTarget_QueuesNothingWhenTheUrlIsRejected()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _sut.InsertTarget("not a url"));

        _targetRepository.DidNotReceive().AddTarget(Arg.Any<Target>());
    }

    [Fact]
    public void RenameTarget_UpdatesTheName()
    {
        Target target = new Target("old-name", "https://example.com", new HealthCheckRecord());

        _sut.RenameTarget(target, "new-name");

        Assert.Equal("new-name", target.Name);
    }

    [Fact]
    public void RenameTarget_RejectsANullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.RenameTarget(null!, "new-name"));
    }

    [Fact]
    public void DeleteTarget_MarksTheTargetAsDeleted()
    {
        Target target = new Target("example", "https://example.com", new HealthCheckRecord());

        _sut.DeleteTarget(target);

        Assert.True(target.IsDeleted);
    }

    [Fact]
    public void DeleteTarget_RejectsANullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.DeleteTarget(null!));
    }
}
