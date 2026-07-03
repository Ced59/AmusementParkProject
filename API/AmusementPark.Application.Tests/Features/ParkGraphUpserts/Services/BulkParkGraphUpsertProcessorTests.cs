using System.Text.Json;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class BulkParkGraphUpsertProcessorTests
{
    [Fact]
    public async Task ProcessAsync_WhenCreateIfMissingIsEnabled_ShouldReturnFailure()
    {
        BulkParkGraphUpsertProcessor processor = new BulkParkGraphUpsertProcessor(CreateUnusedProcessor());
        using JsonDocument document = JsonDocument.Parse("""
        {
          "documentType": "AmusementParkBulkParkGraphUpsert",
          "parks": []
        }
        """);

        ApplicationResult<BulkParkGraphUpsertResult> result = await processor.ProcessAsync(
            new BulkParkGraphUpsertRequest
            {
                CreateIfMissing = true,
                ReplaceCollections = false,
                Document = document.RootElement.Clone(),
            },
            "user-1",
            apply: false,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Validation, result.Errors.Single().Type);
        Assert.Contains("createIfMissing", result.Errors.Single().Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_WhenPreviewWouldCreateEntity_ShouldBlockBulkApply()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Existing Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor singleProcessor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);
        BulkParkGraphUpsertProcessor processor = new BulkParkGraphUpsertProcessor(singleProcessor);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "documentType": "AmusementParkBulkParkGraphUpsert",
          "parks": [
            {
              "identity": { "parkId": "park-1", "name": "Existing Park", "countryCode": "FR" },
              "items": [
                { "name": "New Ride", "category": "Attraction", "type": "RollerCoaster" }
              ]
            }
          ]
        }
        """);

        ApplicationResult<BulkParkGraphUpsertResult> result = await processor.ProcessAsync(
            new BulkParkGraphUpsertRequest
            {
                CreateIfMissing = false,
                ReplaceCollections = false,
                Document = document.RootElement.Clone(),
            },
            "user-1",
            apply: false,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Equal(1, result.Value.Counts.Created);
        Assert.Equal(1, result.Value.Counts.Errors);
        Assert.Contains(result.Value.Errors, static error => error.Contains("update-only", StringComparison.Ordinal));
        Assert.Contains(result.Value.Parks[0].Result.Changes, static change => change.ChangeType == "Created" && change.EntityType == "ParkItem");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ProcessAsync_WhenApplyPreflightFindsLaterInvalidPark_ShouldNotApplyEarlierParks()
    {
        Park firstPark = new Park
        {
            Id = "park-1",
            Name = "First Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Park secondPark = new Park
        {
            Id = "park-2",
            Name = "Second Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPark);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-2", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondPark);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-2", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor singleProcessor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);
        BulkParkGraphUpsertProcessor processor = new BulkParkGraphUpsertProcessor(singleProcessor);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "documentType": "AmusementParkBulkParkGraphUpsert",
          "parks": [
            {
              "identity": { "parkId": "park-1", "name": "First Park", "countryCode": "FR" },
              "park": { "id": "park-1", "name": "First Park Updated", "countryCode": "FR" }
            },
            {
              "identity": { "parkId": "park-2", "name": "Second Park", "countryCode": "FR" },
              "items": [
                { "name": "New Ride", "category": "Attraction", "type": "RollerCoaster" }
              ]
            }
          ]
        }
        """);

        ApplicationResult<BulkParkGraphUpsertResult> result = await processor.ProcessAsync(
            new BulkParkGraphUpsertRequest
            {
                CreateIfMissing = false,
                ReplaceCollections = false,
                Document = document.RootElement.Clone(),
            },
            "user-1",
            apply: true,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsApplied);
        Assert.Null(result.Value.AppliedAtUtc);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("update-only", StringComparison.Ordinal));
        parkRepository.Verify(repository => repository.UpdateAsync(It.IsAny<string>(), It.IsAny<Park>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        historyRepository.Verify(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static ParkGraphUpsertProcessor CreateUnusedProcessor()
    {
        return new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict),
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);
    }
}
