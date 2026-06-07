using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertProcessorTests
{
    [Fact]
    public async Task ApplyAsync_WhenItemIsCreated_ShouldKeepItHiddenAndToReview()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        ParkItem? createdItem = null;
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkItemRepository
            .Setup(value => value.CreateAsync(It.IsAny<ParkItem>(), It.IsAny<CancellationToken>()))
            .Callback<ParkItem, CancellationToken>((item, _) =>
            {
                createdItem = item;
                item.Id = "item-1";
            })
            .ReturnsAsync((ParkItem item, CancellationToken _) => item);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(value => value.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("item-1")), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "items": [
            {
              "key": "coaster-1",
              "name": "Coaster"
            }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = "park-1",
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(createdItem);
        Assert.False(createdItem.IsVisible);
        Assert.Equal(AdminReviewStatus.ToReview, createdItem.AdminReviewStatus);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }
}
