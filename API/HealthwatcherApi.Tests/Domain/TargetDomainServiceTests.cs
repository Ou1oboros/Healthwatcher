using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Implementation;
using HealthwatcherApi.Shared.Common;
using NSubstitute;

namespace HealthwatcherApi.Tests.Domain;

/// <summary>
/// Domain services hold rules that span more than one entity. InsertTarget needs the
/// repository (uniqueness, persistence), so that's substituted; RenameTarget and
/// DeleteTarget act on the entity directly and need nothing at all.
/// </summary>
public class TargetDomainServiceTests
{
    private readonly ITargetRepository _targetRepository = Substitute.For<ITargetRepository>();
    private readonly TargetDomainService _sut;

    public TargetDomainServiceTests()
    {
        _sut = new TargetDomainService(_targetRepository);

        _targetRepository
            .InsertTargetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new Target(callInfo.ArgAt<string>(0), callInfo.ArgAt<string>(1), new HealthCheckRecord()));
    }

    [Fact]
    public async Task InsertTarget_NormalizesABareHostToHttps()
    {
        Target target = await _sut.InsertTarget("github.com");

        Assert.Equal("https://github.com/", target.Url);
    }

    [Fact]
    public async Task InsertTarget_ExtractsANameFromTheHost()
    {
        Target target = await _sut.InsertTarget("https://www.google.com");

        Assert.Equal("google", target.Name);
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

    [Fact]
    public async Task InsertTarget_ThrowsWhenTheRepositoryFailsToCreateIt()
    {
        _targetRepository
            .InsertTargetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Target?)null);

        await Assert.ThrowsAsync<BusinessException>(() => _sut.InsertTarget("https://github.com"));
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
