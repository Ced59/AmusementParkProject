using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class UpdateParkItemCommandHandlerPublicationTests
{
    [Fact]
    public async Task HandleAsync_WhenHiddenItemBecomesVisibleButIncomplete_ShouldBlockPublication()
    {
        ParkItem existing = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Draft",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            IsVisible = false,
        };

        ParkItem update = new ParkItem
        {
            ParkId = "park-1",
            Name = "Draft",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            IsVisible = true,
        };

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1" });

        ParkItemReferenceValidator validator = new ParkItemReferenceValidator(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict));

        UpdateParkItemCommandHandler handler = new UpdateParkItemCommandHandler(
            parkItemRepository.Object,
            validator,
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            new ParkItemContentQualityService(),
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict));

        ApplicationResult<ParkItem> result = await handler.HandleAsync(new UpdateParkItemCommand("item-1", update), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "park-item.publication.incomplete");
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }
}
