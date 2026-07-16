using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Handlers;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.StandaloneAttractions.Handlers;

public sealed class MigrateParkToStandaloneAttractionCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkAndItemExist_ShouldCreateStandaloneAttractionAndRetireLegacyEntities()
    {
        Park sourcePark = new Park
        {
            Id = "park-1",
            Name = "Legacy Park",
            CountryCode = "it",
            OperatorId = "operator-1",
            WebsiteUrl = "https://example.test",
            Street = "Regione Molino 18",
            City = "Bardonecchia",
            PostalCode = "10052",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        sourcePark.SetPosition(45.07d, 6.70d);
        ParkItem sourceItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Bardonecchia Alpine Coaster",
            Type = ParkItemType.RollerCoaster,
            Subtype = "Alpine coaster",
            AttractionDetails = new AttractionDetails
            {
                ManufacturerId = "manufacturer-1",
                Model = "Alpine Coaster",
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        sourceItem.SetPosition(45.08d, 6.71d);
        StandaloneAttraction? createdAttraction = null;
        Park? retiredPark = null;
        ParkItem? retiredItem = null;
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourcePark);
        parkRepository
            .Setup(repository => repository.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<string, Park, CancellationToken>((_, park, _) => retiredPark = park)
            .ReturnsAsync((string _, Park park, CancellationToken _) => park);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceItem);
        parkItemRepository
            .Setup(repository => repository.UpdateAsync("item-1", It.IsAny<ParkItem>(), It.IsAny<CancellationToken>()))
            .Callback<string, ParkItem, CancellationToken>((_, item, _) => retiredItem = item)
            .ReturnsAsync((string _, ParkItem item, CancellationToken _) => item);
        Mock<IStandaloneAttractionRepository> standaloneAttractionRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneAttractionRepository
            .Setup(repository => repository.FindByLegacyAsync("park-1", "item-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StandaloneAttraction?)null);
        standaloneAttractionRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StandaloneAttraction?)null);
        standaloneAttractionRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<StandaloneAttraction>(), It.IsAny<CancellationToken>()))
            .Callback<StandaloneAttraction, CancellationToken>((attraction, _) =>
            {
                attraction.Id = "standalone-1";
                createdAttraction = attraction;
            })
            .ReturnsAsync((StandaloneAttraction attraction, CancellationToken _) => attraction);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, "standalone-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.ParkItems, "item-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        MigrateParkToStandaloneAttractionCommandHandler handler = new MigrateParkToStandaloneAttractionCommandHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            standaloneAttractionRepository.Object,
            searchProjectionWriter.Object);

        ApplicationResult<StandaloneAttraction> result = await handler.HandleAsync(
            new MigrateParkToStandaloneAttractionCommand(new StandaloneAttractionMigrationRequest
            {
                LegacyParkId = " park-1 ",
                LegacyParkItemId = " item-1 ",
                RetireLegacyPark = true,
                RetireLegacyParkItem = true,
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdAttraction);
        Assert.Equal("Bardonecchia Alpine Coaster", createdAttraction!.Name);
        Assert.Equal("IT", createdAttraction.CountryCode);
        Assert.Equal(ParkItemType.RollerCoaster, createdAttraction.Type);
        Assert.Equal("Alpine coaster", createdAttraction.Subtype);
        Assert.Equal("operator-1", createdAttraction.OperatorId);
        Assert.Equal("park-1", createdAttraction.LegacyParkId);
        Assert.Equal("item-1", createdAttraction.LegacyParkItemId);
        Assert.Equal("manufacturer-1", createdAttraction.AttractionDetails?.ManufacturerId);
        Assert.Equal(45.08d, createdAttraction.Position?.Latitude);
        Assert.False(createdAttraction.IsVisible);
        Assert.Equal(AdminReviewStatus.ToReview, createdAttraction.AdminReviewStatus);
        Assert.NotNull(retiredPark);
        Assert.False(retiredPark!.IsVisible);
        Assert.Equal(AdminReviewStatus.NotRelevant, retiredPark.AdminReviewStatus);
        Assert.NotNull(retiredItem);
        Assert.False(retiredItem!.IsVisible);
        Assert.Equal(AdminReviewStatus.NotRelevant, retiredItem.AdminReviewStatus);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        standaloneAttractionRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
    }
}
