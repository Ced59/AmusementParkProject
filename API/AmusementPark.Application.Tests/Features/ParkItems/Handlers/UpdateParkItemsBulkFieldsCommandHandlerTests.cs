using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class UpdateParkItemsBulkFieldsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenIdsAreEmpty_ShouldReturnFailure()
    {
        Mock<IParkItemRepository> repository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        UpdateParkItemsBulkFieldsCommandHandler handler = new UpdateParkItemsBulkFieldsCommandHandler(repository.Object, searchProjectionWriter.Object, new ParkItemContentQualityService());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParkItemsBulkFieldsCommand(Array.Empty<string>(), true, "zone-1", null, null, false, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "validation.required");
        repository.VerifyNoOtherCalls();
        searchProjectionWriter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNoFieldActionIsProvided_ShouldReturnFailure()
    {
        Mock<IParkItemRepository> repository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        UpdateParkItemsBulkFieldsCommandHandler handler = new UpdateParkItemsBulkFieldsCommandHandler(repository.Object, searchProjectionWriter.Object, new ParkItemContentQualityService());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParkItemsBulkFieldsCommand(new[] { "item-1" }, false, null, null, null, false, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "validation.required");
        repository.VerifyNoOtherCalls();
        searchProjectionWriter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenFieldsAreProvided_ShouldNormalizeIdsUpdateRepositoryAndRefreshSearch()
    {
        Mock<IParkItemRepository> repository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        IReadOnlyCollection<string> expectedIds = new[] { "item-1", "item-2" };
        IReadOnlyCollection<ParkItem> publishableItems = new[]
        {
            new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                ZoneId = "zone-1",
                Name = "Item 1",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.RollerCoaster,
                Descriptions = new List<AmusementPark.Core.Localization.LocalizedText>
                {
                    new AmusementPark.Core.Localization.LocalizedText("fr", "Description FR"),
                },
            },
            new ParkItem
            {
                Id = "item-2",
                ParkId = "park-1",
                ZoneId = "zone-1",
                Name = "Item 2",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.RollerCoaster,
                Descriptions = new List<AmusementPark.Core.Localization.LocalizedText>
                {
                    new AmusementPark.Core.Localization.LocalizedText("fr", "Description FR"),
                },
            },
        };

        repository
            .Setup(item => item.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(publishableItems);

        repository
            .Setup(item => item.UpdateBulkFieldsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIds)),
                true,
                "zone-1",
                ParkItemCategory.Attraction,
                ParkItemType.RollerCoaster,
                true,
                "manufacturer-1",
                true,
                AdminReviewStatus.Validated,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        searchProjectionWriter
            .Setup(item => item.UpsertManyAsync(
                SearchProjectionResourceTypes.ParkItems,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIds)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpdateParkItemsBulkFieldsCommandHandler handler = new UpdateParkItemsBulkFieldsCommandHandler(repository.Object, searchProjectionWriter.Object, new ParkItemContentQualityService());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParkItemsBulkFieldsCommand(
                new[] { " item-1 ", "item-2", "item-1", " " },
                true,
                " zone-1 ",
                ParkItemCategory.Attraction,
                ParkItemType.RollerCoaster,
                true,
                " manufacturer-1 ",
                true,
                AdminReviewStatus.Validated),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(2, result.Value.UpdatedCount);
        repository.VerifyAll();
        searchProjectionWriter.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenMakingIncompleteItemsVisible_ShouldReturnPublicationFailure()
    {
        Mock<IParkItemRepository> repository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        IReadOnlyCollection<string> expectedIds = new[] { "item-1" };
        IReadOnlyCollection<ParkItem> incompleteItems = new[]
        {
            new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Incomplete",
                Category = ParkItemCategory.Attraction,
                Type = ParkItemType.Attraction,
                IsVisible = false,
            },
        };

        repository
            .Setup(item => item.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(incompleteItems);

        UpdateParkItemsBulkFieldsCommandHandler handler = new UpdateParkItemsBulkFieldsCommandHandler(repository.Object, searchProjectionWriter.Object, new ParkItemContentQualityService());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParkItemsBulkFieldsCommand(new[] { "item-1" }, false, null, null, null, false, null, true, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "park-item.publication.incomplete");
        repository.VerifyAll();
        searchProjectionWriter.VerifyNoOtherCalls();
    }
}
