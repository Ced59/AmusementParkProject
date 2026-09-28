using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Handlers;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkZones.Handlers;

public sealed class ParkZoneSitemapRefreshTests
{
    [Fact]
    public async Task Create_WhenPersistenceSucceeds_RefreshesSitemap()
    {
        ParkZone zone = CreateZone();
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> scheduler = CreateScheduler();
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        zoneRepository
            .Setup(repository => repository.CreateAsync(zone, CancellationToken.None))
            .ReturnsAsync(zone);
        CreateParkZoneCommandHandler handler = new CreateParkZoneCommandHandler(
            zoneRepository.Object,
            parkRepository.Object,
            scheduler.Object);

        ApplicationResult<ParkZone> result = await handler.HandleAsync(
            new CreateParkZoneCommand(zone),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        zoneRepository.VerifyAll();
        parkRepository.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task Update_WhenPersistenceSucceeds_RefreshesSitemap()
    {
        ParkZone existing = CreateZone();
        ParkZone update = CreateZone();
        update.IsVisible = false;
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> scheduler = CreateScheduler();
        zoneRepository
            .Setup(repository => repository.GetByIdAsync("zone-1", CancellationToken.None))
            .ReturnsAsync(existing);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        zoneRepository
            .Setup(repository => repository.UpdateAsync(
                "zone-1",
                It.Is<ParkZone>(zone => !zone.IsVisible),
                CancellationToken.None))
            .ReturnsAsync(update);
        UpdateParkZoneCommandHandler handler = new UpdateParkZoneCommandHandler(
            zoneRepository.Object,
            parkRepository.Object,
            scheduler.Object);

        ApplicationResult<ParkZone> result = await handler.HandleAsync(
            new UpdateParkZoneCommand("zone-1", update),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        zoneRepository.VerifyAll();
        parkRepository.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task Delete_WhenPersistenceSucceeds_RefreshesSitemap()
    {
        ParkZone existing = CreateZone();
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> scheduler = CreateScheduler();
        zoneRepository
            .Setup(repository => repository.GetByIdAsync("zone-1", CancellationToken.None))
            .ReturnsAsync(existing);
        zoneRepository
            .Setup(repository => repository.DeleteAsync("zone-1", CancellationToken.None))
            .ReturnsAsync(true);
        DeleteParkZoneCommandHandler handler = new DeleteParkZoneCommandHandler(
            zoneRepository.Object,
            scheduler.Object);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteParkZoneCommand("zone-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        zoneRepository.VerifyAll();
        scheduler.VerifyAll();
    }

    private static Mock<ISeoSitemapRefreshScheduler> CreateScheduler()
    {
        Mock<ISeoSitemapRefreshScheduler> scheduler =
            new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(candidate => candidate.RequestRefreshAsync(CancellationToken.None))
            .Returns(Task.CompletedTask);
        return scheduler;
    }

    private static ParkZone CreateZone()
    {
        return new ParkZone
        {
            Id = "zone-1",
            ParkId = "park-1",
            Name = "Zone",
            IsVisible = true
        };
    }
}
