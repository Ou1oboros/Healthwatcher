using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Application.Contracts.DTOs.Target;
using HealthwatcherApi.Application.Exceptions;
using HealthwatcherApi.Application.Services.Implementation;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Shared.Common;
using NSubstitute;

namespace HealthwatcherApi.Tests.Application;

// Application services orchestrate: load, delegate to the domain, save, map out. Everything
// they depend on is substituted, so nothing here touches a database or retests a domain rule.
public class TargetServiceTests
{
    private readonly ITargetRepository _targetRepository = Substitute.For<ITargetRepository>();
    private readonly ITargetDomainService _targetDomainService = Substitute.For<ITargetDomainService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TargetService _sut;

    public TargetServiceTests()
    {
        _sut = new TargetService(_targetRepository, _targetDomainService, _unitOfWork);
    }

    private static Target NewTarget(string name = "example", string url = "https://example.com/") =>
        new Target(name, url, new HealthCheckRecord());

    [Fact]
    public async Task GetTargetById_ThrowsWhenTheTargetDoesNotExist()
    {
        _targetRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Target?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetTargetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetTargetById_MapsTheEntity()
    {
        Target target = NewTarget();
        _targetRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(target);

        TargetDto dto = await _sut.GetTargetById(target.Id);

        Assert.Equal(target.Id, dto.Id);
        Assert.Equal(target.Name, dto.Name);
        Assert.Equal(target.Url, dto.Url);
    }

    [Fact]
    public async Task GetTargets_ReturnsAPagedResultMatchingTheRequestedPage()
    {
        List<Target> items = [NewTarget("a"), NewTarget("b")];
        _targetRepository
            .GetPagedAsync(2, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Target>)items, 25));

        PagedResult<PreviewTargetDto> result = await _sut.GetTargets(new PageRequest { PageIndex = 2, PageSize = 10 });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageIndex);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetTargetHistory_ThrowsWhenTheTargetDoesNotExist()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetTargetHistory(Guid.NewGuid(), new PageRequest(), withinHours: null));
    }

    [Fact]
    public async Task GetTargetHistory_PassesNoLowerBoundWhenNoWindowIsRequested()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _targetRepository
            .GetHistoryPagedAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<TargetHistory>)[], 0));

        await _sut.GetTargetHistory(Guid.NewGuid(), new PageRequest(), withinHours: null);

        await _targetRepository.Received(1).GetHistoryPagedAsync(
            Arg.Any<Guid>(), null, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTargetHistory_ComputesTheLowerBoundFromTheRequestedWindow()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _targetRepository
            .GetHistoryPagedAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<TargetHistory>)[], 0));

        DateTimeOffset before = DateTimeOffset.UtcNow.AddHours(-6);
        await _sut.GetTargetHistory(Guid.NewGuid(), new PageRequest(), withinHours: 6);
        DateTimeOffset after = DateTimeOffset.UtcNow.AddHours(-6);

        await _targetRepository.Received(1).GetHistoryPagedAsync(
            Arg.Any<Guid>(),
            Arg.Is<DateTimeOffset?>(since => since >= before && since <= after),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTargetUptime_ThrowsWhenTheTargetDoesNotExist()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetTargetUptime(Guid.NewGuid(), 24));
    }

    [Fact]
    public async Task GetTargetUptime_ReturnsZeroPercentWhenThereAreNoChecks()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _targetRepository
            .GetCheckCountsAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((0, 0));

        TargetUptimeDto uptime = await _sut.GetTargetUptime(Guid.NewGuid(), 24);

        Assert.Equal(0, uptime.UptimePercentage);
    }

    [Fact]
    public async Task GetTargetUptime_ComputesThePercentage()
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _targetRepository
            .GetCheckCountsAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((3, 4));

        TargetUptimeDto uptime = await _sut.GetTargetUptime(Guid.NewGuid(), 24);

        Assert.Equal(75, uptime.UptimePercentage);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(999_999, 24 * 7)]
    public async Task GetTargetUptime_ClampsTheRequestedWindow(int requested, int expected)
    {
        _targetRepository.ExistsByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _targetRepository
            .GetCheckCountsAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((0, 0));

        TargetUptimeDto uptime = await _sut.GetTargetUptime(Guid.NewGuid(), requested);

        Assert.Equal(expected, uptime.WindowHours);
    }

    [Fact]
    public async Task InsertTarget_DelegatesToTheDomainServiceSavesAndMapsTheResult()
    {
        Target created = NewTarget("github", "https://github.com/");
        _targetDomainService.InsertTarget("github.com", Arg.Any<CancellationToken>()).Returns(created);

        PreviewTargetDto dto = await _sut.InsertTarget(new InsertTargetDto { Url = "github.com" });

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal(created.Id, dto.Id);
        Assert.Equal("github", dto.Name);
        Assert.Equal("https://github.com/", dto.Url);
    }

    [Fact]
    public async Task RenameTarget_ThrowsWhenTheTargetDoesNotExist()
    {
        _targetRepository.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Target?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.RenameTarget(Guid.NewGuid(), new RenameTargetDto { Name = "new-name" }));
    }

    [Fact]
    public async Task RenameTarget_SavesExactlyOnce()
    {
        Target target = NewTarget();
        _targetRepository.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(target);

        await _sut.RenameTarget(target.Id, new RenameTargetDto { Name = "new-name" });

        _targetDomainService.Received(1).RenameTarget(target, "new-name");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTarget_ThrowsWhenTheTargetDoesNotExist()
    {
        _targetRepository.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Target?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteTarget(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteTarget_SavesExactlyOnce()
    {
        Target target = NewTarget();
        _targetRepository.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(target);

        await _sut.DeleteTarget(target.Id);

        _targetDomainService.Received(1).DeleteTarget(target);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
