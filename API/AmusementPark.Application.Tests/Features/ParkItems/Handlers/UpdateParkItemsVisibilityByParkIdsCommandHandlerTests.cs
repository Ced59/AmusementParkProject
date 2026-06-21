using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class UpdateParkItemsVisibilityByParkIdsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenMakingParkItemsVisible_ShouldUpdateIncompleteItemsToo()
    {
        Mock<IParkItemRepository> repository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        IReadOnlyCollection<string> expectedParkIds = new[] { "park-1" };
        IReadOnlyCollection<string> expectedItemIds = new[] { "item-1", "item-2" };
        IReadOnlyCollection<ParkItem> previousItems = new[]
        {
            new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Incomplete service",
                Category = ParkItemCategory.Service,
                Type = ParkItemType.Other,
                IsVisible = false,
            },
            new ParkItem
            {
                Id = "item-2",
                ParkId = "park-1",
                Name = "Incomplete attraction",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.Attraction,
                IsVisible = false,
            },
        };
        IReadOnlyCollection<ParkItem> currentItems = previousItems
            .Select(static item => new ParkItem
            {
                Id = item.Id,
                ParkId = item.ParkId,
                Name = item.Name,
                Category = item.Category,
                Type = item.Type,
                IsVisible = true,
            })
            .ToList();

        repository
            .Setup(item => item.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkIds => parkIds.SequenceEqual(expectedParkIds)),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousItems);

        repository
            .Setup(item => item.UpdateBulkAdministrationAsync(
                It.Is<IReadOnlyCollection<string>>(parkItemIds => parkItemIds.SequenceEqual(expectedItemIds)),
                true,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        searchProjectionWriter
            .Setup(item => item.UpsertManyAsync(
                SearchProjectionResourceTypes.ParkItems,
                It.Is<IReadOnlyCollection<string>>(parkItemIds => parkItemIds.SequenceEqual(expectedItemIds)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repository
            .Setup(item => item.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(parkItemIds => parkItemIds.SequenceEqual(expectedItemIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentItems);

        publicSeoUpdateNotifier
            .Setup(item => item.NotifyAsync(
                It.IsAny<PublicSeoUpdate>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpdateParkItemsVisibilityByParkIdsCommandHandler handler = new UpdateParkItemsVisibilityByParkIdsCommandHandler(
            repository.Object,
            searchProjectionWriter.Object,
            publicSeoUpdateNotifier.Object);

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParkItemsVisibilityByParkIdsCommand(new[] { " park-1 ", "park-1", " " }, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(2, result.Value.UpdatedCount);
        repository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }
}
