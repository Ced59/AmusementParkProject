using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertProcessorTests
{
    [Fact]
    public async Task ApplyAsync_WhenParkScopedDocumentHasInvalidPark_ShouldNotWriteReferences()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("missing-park", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Park?)null);

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            manufacturerRepository.Object,
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "identity": { "parkId": "missing-park" },
          "references": {
            "manufacturers": [
              { "key": "maker", "name": "Draft Manufacturer" }
            ]
          },
          "items": [
            { "key": "ride", "name": "Draft Ride", "category": "Attraction", "type": "RollerCoaster" }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = "missing-park",
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.False(result.IsSuccess);
        manufacturerRepository.Verify(value => value.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
        manufacturerRepository.Verify(value => value.CreateAsync(It.IsAny<AttractionManufacturer>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        historyRepository.VerifyAll();
        manufacturerRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenExistingImageOwnerCannotBeResolved_ShouldSkipOwnerUpdate()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Image image = new Image
        {
            Id = "image-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            OriginalFileName = "photo.jpg",
            IsPublished = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "images": [
            {
              "imageId": "image-1",
              "ownerType": "ParkItem",
              "ownerKey": "missing-item"
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
        Assert.Contains(result.Value!.Warnings, static warning => warning.Contains("owner could not be resolved", StringComparison.Ordinal));
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value.Changes, change => change.EntityType == "Image");
        Assert.Equal("Skipped", imageChange.ChangeType);
        imageRepository.Verify(value => value.UpdateMetadataAsync(It.IsAny<string>(), It.IsAny<ImageMetadataUpdate>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenDeletingImageOwnedByAnotherParkItem_ShouldRejectWithoutDeleting()
    {
        Park targetPark = new Park
        {
            Id = "park-1",
            Name = "Target Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Image image = new Image
        {
            Id = "image-other",
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = "item-other",
            Category = ImageCategory.ParkItem,
            OriginalFileName = "other.jpg",
            IsPublished = true,
        };
        ParkItem otherParkItem = new ParkItem
        {
            Id = "item-other",
            ParkId = "park-2",
            Name = "Other Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPark);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPark);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByIdAsync("item-other", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherParkItem);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-other", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "suppr": [
            { "entityType": "Image", "id": "image-other" }
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
        Assert.Contains(result.Value!.Errors, static error => error.Contains("n'appartient pas au parc cible", StringComparison.Ordinal));
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value.Changes, change => change.EntityType == "Image");
        Assert.Equal("Skipped", imageChange.ChangeType);
        imageRepository.Verify(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenDeletingParkItemOutsideTargetPark_ShouldRejectWithoutDeleting()
    {
        Park targetPark = new Park
        {
            Id = "park-1",
            Name = "Target Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        ParkItem otherParkItem = new ParkItem
        {
            Id = "item-other",
            ParkId = "park-2",
            Name = "Other Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPark);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetPark);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByIdAsync("item-other", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherParkItem);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "suppr": [
            { "entityType": "ParkItem", "id": "item-other" }
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
        Assert.Contains(result.Value!.Errors, static error => error.Contains("n'appartient pas au parc cible", StringComparison.Ordinal));
        ParkGraphUpsertChange itemChange = Assert.Single(result.Value.Changes, change => change.EntityType == "ParkItem");
        Assert.Equal("Skipped", itemChange.ChangeType);
        parkItemRepository.Verify(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenParkItemHistoryContextIsNull_ShouldFallbackUnlessExplicitExternal()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        List<HistoryEvent> savedEvents = new List<HistoryEvent>();

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(value => value.GetByOwnerKeyAsync(HistoryEntityType.ParkItem, "item-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        historyEventRepository
            .Setup(value => value.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .Callback<HistoryEvent, CancellationToken>((historyEvent, _) =>
            {
                historyEvent.Id = $"history-{savedEvents.Count + 1}";
                savedEvents.Add(historyEvent);
            })
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) => historyEvent);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            historyEventRepository: historyEventRepository.Object);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "historyEvents": [
            {
              "owner": "parkItem",
              "ownerId": "item-1",
              "key": "miralooping-mirapolis",
              "eventType": "Opening",
              "date": "1988-07"
            },
            {
              "owner": "parkItem",
              "ownerId": "item-1",
              "key": "miralooping-spreepark",
              "eventType": "RelocationArrival",
              "date": "1992",
              "contextParkId": "none"
            },
            {
              "owner": "parkItem",
              "ownerId": "item-1",
              "key": "miralooping-imported-null-context",
              "eventType": "ThemeChange",
              "date": "1993",
              "contextParkId": null
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
        Assert.Empty(result.Value!.Errors);
        Assert.Equal(3, savedEvents.Count);

        HistoryEvent localEvent = Assert.Single(savedEvents, historyEvent => historyEvent.Key == "miralooping-mirapolis");
        Assert.Equal("item-1", localEvent.ParkItemId);
        Assert.Equal("park-1", localEvent.ContextParkId);

        HistoryEvent externalEvent = Assert.Single(savedEvents, historyEvent => historyEvent.Key == "miralooping-spreepark");
        Assert.Equal("item-1", externalEvent.ParkItemId);
        Assert.Null(externalEvent.ContextParkId);
        Assert.Null(externalEvent.ParkId);

        HistoryEvent nullContextEvent = Assert.Single(savedEvents, historyEvent => historyEvent.Key == "miralooping-imported-null-context");
        Assert.Equal("item-1", nullContextEvent.ParkItemId);
        Assert.Equal("park-1", nullContextEvent.ContextParkId);

        parkRepository.VerifyAll();
        historyEventRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenHistoryLocalizedFieldsUseCompactPluralObjects_ShouldImportTexts()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        List<HistoryEvent> savedEvents = new List<HistoryEvent>();

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(value => value.GetByOwnerKeyAsync(HistoryEntityType.Park, "park-1", "mirapolis-opening", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        historyEventRepository
            .Setup(value => value.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .Callback<HistoryEvent, CancellationToken>((historyEvent, _) =>
            {
                historyEvent.Id = $"history-{savedEvents.Count + 1}";
                savedEvents.Add(historyEvent);
            })
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) => historyEvent);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            historyEventRepository: historyEventRepository.Object);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "historyEvents": [
            {
              "owner": "park",
              "ownerId": "park-1",
              "key": "mirapolis-opening",
              "eventType": "Opening",
              "date": "1987-05-20",
              "isVisible": true,
              "isMajor": true,
              "titles": {
                "fr": "Ouverture de Mirapolis",
                "en": "Mirapolis opens"
              },
              "summaries": {
                "fr": "Mirapolis ouvre au public."
              },
              "article": {
                "slug": "ouverture-mirapolis",
                "isPublished": true,
                "titles": {
                  "fr": "20 mai 1987 : Mirapolis ouvre ses portes"
                },
                "subtitles": {
                  "fr": "Un lancement spectaculaire."
                },
                "summaries": {
                  "fr": "Le parc ouvre autour de Gargantua."
                },
                "blocks": [
                  {
                    "id": "intro",
                    "type": "Paragraph",
                    "sortOrder": 1,
                    "texts": {
                      "fr": "Mirapolis ouvre au public le 20 mai 1987.",
                      "en": "Mirapolis opens to the public on 20 May 1987."
                    }
                  },
                  {
                    "id": "photo",
                    "type": "Image",
                    "sortOrder": 2,
                    "imageId": "image-1",
                    "captions": {
                      "fr": "Logo de Mirapolis."
                    }
                  }
                ],
                "sources": [
                  {
                    "label": "Source",
                    "url": "https://example.test/mirapolis",
                    "accessedAt": "2026-06-30"
                  }
                ]
              }
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
        Assert.Empty(result.Value!.Errors);

        HistoryEvent savedEvent = Assert.Single(savedEvents);
        Assert.Contains(savedEvent.Titles, static text => text.LanguageCode == "fr" && text.Value == "Ouverture de Mirapolis");
        Assert.Contains(savedEvent.Summaries, static text => text.LanguageCode == "fr" && text.Value == "Mirapolis ouvre au public.");
        Assert.NotNull(savedEvent.Article);
        Assert.Contains(savedEvent.Article!.Titles, static text => text.LanguageCode == "fr" && text.Value == "20 mai 1987 : Mirapolis ouvre ses portes");
        Assert.Contains(savedEvent.Article.Subtitles, static text => text.LanguageCode == "fr" && text.Value == "Un lancement spectaculaire.");
        Assert.Contains(savedEvent.Article.Summaries, static text => text.LanguageCode == "fr" && text.Value == "Le parc ouvre autour de Gargantua.");
        Assert.Contains(savedEvent.Article.Blocks[0].Texts, static text => text.LanguageCode == "fr" && text.Value == "Mirapolis ouvre au public le 20 mai 1987.");
        Assert.Contains(savedEvent.Article.Blocks[1].Captions, static text => text.LanguageCode == "fr" && text.Value == "Logo de Mirapolis.");

        parkRepository.VerifyAll();
        historyEventRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenHistoryReferencesImportedImageKey_ShouldResolveImageIds()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Image importedImage = new Image
        {
            Id = "image-imported",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            SourceUrl = "https://example.test/mirapolis.jpg",
            IsPublished = true,
        };
        List<HistoryEvent> savedEvents = new List<HistoryEvent>();

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://example.test/mirapolis.jpg", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        imageRepository
            .Setup(value => value.UpdateMetadataAsync("image-imported", It.IsAny<ImageMetadataUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        remoteImageImporter
            .Setup(value => value.ImportAsync(It.Is<RemoteImageImportRequest>(request =>
                request.SourceUrl == "https://example.test/mirapolis.jpg" &&
                request.OwnerType == ImageOwnerType.Park &&
                request.OwnerId == "park-1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(value => value.GetByOwnerKeyAsync(HistoryEntityType.Park, "park-1", "mirapolis-opening", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        historyEventRepository
            .Setup(value => value.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .Callback<HistoryEvent, CancellationToken>((historyEvent, _) =>
            {
                historyEvent.Id = $"history-{savedEvents.Count + 1}";
                savedEvents.Add(historyEvent);
            })
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) => historyEvent);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            historyEventRepository: historyEventRepository.Object);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "key": "mirapolis-aerial",
              "sourceUrl": "https://example.test/mirapolis.jpg",
              "ownerKey": "park",
              "ownerType": "Park",
              "category": "Photo",
              "description": "Mirapolis aerial view",
              "isPublished": true,
              "altTexts": {
                "fr": "Vue aérienne de Mirapolis"
              }
            }
          ],
          "historyEvents": [
            {
              "owner": "park",
              "ownerId": "park-1",
              "key": "mirapolis-opening",
              "eventType": "Opening",
              "date": "1987-05-20",
              "mainImageKey": "mirapolis-aerial",
              "article": {
                "title": {
                  "fr": "Ouverture de Mirapolis"
                },
                "mainImageKey": "mirapolis-aerial",
                "blocks": [
                  {
                    "id": "photo",
                    "type": "Image",
                    "sortOrder": 1,
                    "imageKey": "mirapolis-aerial",
                    "captions": {
                      "fr": "Vue aérienne de Mirapolis."
                    }
                  },
                  {
                    "id": "gallery",
                    "type": "Gallery",
                    "sortOrder": 2,
                    "imageKeys": ["mirapolis-aerial"]
                  }
                ]
              }
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
        Assert.Empty(result.Value!.Errors);
        HistoryEvent savedEvent = Assert.Single(savedEvents);
        Assert.Equal("image-imported", savedEvent.MainImageId);
        Assert.NotNull(savedEvent.Article);
        Assert.Equal("image-imported", savedEvent.Article!.MainImageId);
        Assert.Equal("image-imported", savedEvent.Article.Blocks[0].ImageId);
        Assert.Equal(new[] { "image-imported" }, savedEvent.Article.Blocks[1].ImageIds);

        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        remoteImageImporter.VerifyAll();
        historyEventRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenParkLifecycleDatesAreProvided_ShouldPersistAndNotifySeo()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        Park? updatedPark = null;
        PublicSeoUpdate? seoUpdate = null;

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<string, Park, CancellationToken>((id, value, cancellationToken) => updatedPark = value)
            .ReturnsAsync((string id, Park value, CancellationToken cancellationToken) => value);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Callback<PublicSeoUpdate, CancellationToken>((update, _) => seoUpdate = update)
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "park": {
            "openingDate": "1987-05-20",
            "closingDate": "1991-10-20",
            "openingDateText": "1987-05-20",
            "closingDateText": "1991-10-20"
          }
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
        Assert.NotNull(updatedPark);
        Assert.Equal(new DateTime(1987, 5, 20), updatedPark!.OpeningDate?.Date);
        Assert.Equal(new DateTime(1991, 10, 20), updatedPark.ClosingDate?.Date);
        Assert.Equal("1987-05-20", updatedPark.OpeningDateText);
        Assert.Equal("1991-10-20", updatedPark.ClosingDateText);
        ParkGraphUpsertChange parkChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Park");
        Assert.Equal("Updated", parkChange.ChangeType);
        Assert.Contains(parkChange.Fields, field => field.Field == "openingDate");
        Assert.Contains(parkChange.Fields, field => field.Field == "closingDate");
        Assert.NotNull(seoUpdate);
        Assert.False(seoUpdate!.SuppressSitemapRefresh);
        Assert.Contains(seoUpdate.PreviousParks, previousPark => previousPark.Id == "park-1" && previousPark.OpeningDate == null && previousPark.ClosingDate == null);
        Assert.Contains(seoUpdate.CurrentParks, currentPark =>
            currentPark.Id == "park-1" &&
            currentPark.OpeningDate?.Date == new DateTime(1987, 5, 20) &&
            currentPark.ClosingDate?.Date == new DateTime(1991, 10, 20));
        parkRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenZoneNameChanges_ShouldNotifySeoWithPreviousAndCurrentZoneSnapshots()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        ParkZone zone = new ParkZone
        {
            Id = "zone-1",
            ParkId = "park-1",
            Name = "Old Zone",
            IsVisible = true,
        };
        PublicSeoUpdate? seoUpdate = null;

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, Park value, CancellationToken cancellationToken) => value);

        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkZoneRepository
            .Setup(value => value.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { zone });
        parkZoneRepository
            .Setup(value => value.UpdateAsync("zone-1", It.IsAny<ParkZone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, ParkZone value, CancellationToken cancellationToken) => value);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Callback<PublicSeoUpdate, CancellationToken>((update, _) => seoUpdate = update)
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            parkZoneRepository.Object,
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "zones": [
            {
              "id": "zone-1",
              "name": "New Zone"
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
        ParkGraphUpsertChange zoneChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkZone");
        Assert.Equal("Updated", zoneChange.ChangeType);
        Assert.NotNull(seoUpdate);
        Assert.Contains(seoUpdate!.PreviousParkZones, previousZone => previousZone.Id == "zone-1" && previousZone.Name == "Old Zone" && previousZone.IsVisible);
        Assert.Contains(seoUpdate.CurrentParkZones, currentZone => currentZone.Id == "zone-1" && currentZone.Name == "New Zone" && currentZone.IsVisible);
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenZoneIsCreated_ShouldNotNotifySeoWithPreviousZoneSnapshot()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Magic Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        PublicSeoUpdate? seoUpdate = null;

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, Park value, CancellationToken cancellationToken) => value);

        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkZoneRepository
            .Setup(value => value.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        parkZoneRepository
            .Setup(value => value.CreateAsync(It.IsAny<ParkZone>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkZone value, CancellationToken cancellationToken) =>
            {
                value.Id = "zone-new";
                return value;
            });

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Callback<PublicSeoUpdate, CancellationToken>((update, _) => seoUpdate = update)
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            parkZoneRepository.Object,
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "zones": [
            {
              "key": "hidden-zone",
              "name": "Backstage",
              "isVisible": false
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
        ParkGraphUpsertChange zoneChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkZone");
        Assert.Equal("Created", zoneChange.ChangeType);
        Assert.NotNull(seoUpdate);
        Assert.Empty(seoUpdate!.PreviousParkZones);
        Assert.Contains(seoUpdate.CurrentParkZones, currentZone => currentZone.Id == "zone-new" && currentZone.Name == "Backstage" && !currentZone.IsVisible);
        parkRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenLifecycleDatesUseYearOnly_ShouldStoreDateTextWithoutInventingExactDate()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            OpeningDate = new DateTime(1987, 5, 20),
            OpeningDateText = "1987-05-20",
            ClosingDate = new DateTime(1991, 10, 20),
            ClosingDateText = "1991-10-20",
        };

        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails
            {
                OpeningDate = new DateTime(1988, 6, 1),
                OpeningDateText = "1988-06-01",
                ClosingDate = new DateTime(1992, 9, 30),
                ClosingDateText = "1992-09-30",
            },
        };

        Park? updatedPark = null;
        ParkItem? updatedItem = null;
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback<string, Park, CancellationToken>((id, value, cancellationToken) => updatedPark = value)
            .ReturnsAsync((string id, Park value, CancellationToken cancellationToken) => value);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        parkItemRepository
            .Setup(value => value.UpdateAsync("item-1", It.IsAny<ParkItem>(), It.IsAny<CancellationToken>()))
            .Callback<string, ParkItem, CancellationToken>((id, value, cancellationToken) => updatedItem = value)
            .ReturnsAsync((string id, ParkItem value, CancellationToken cancellationToken) => value);

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

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "park": {
            "openingDate": "1987",
            "closingDate": "1991"
          },
          "items": [
            {
              "id": "item-1",
              "name": "Ride",
              "attractionDetails": {
                "openingDate": "1988",
                "closingDate": "1992"
              }
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
        Assert.NotNull(updatedPark);
        Assert.Null(updatedPark!.OpeningDate);
        Assert.Null(updatedPark.ClosingDate);
        Assert.Equal("1987", updatedPark.OpeningDateText);
        Assert.Equal("1991", updatedPark.ClosingDateText);
        Assert.NotNull(updatedItem?.AttractionDetails);
        Assert.Null(updatedItem!.AttractionDetails!.OpeningDate);
        Assert.Null(updatedItem.AttractionDetails.ClosingDate);
        Assert.Equal("1988", updatedItem.AttractionDetails.OpeningDateText);
        Assert.Equal("1992", updatedItem.AttractionDetails.ClosingDateText);

        ParkGraphUpsertChange parkChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Park");
        Assert.Contains(parkChange.Fields, field => field.Field == "openingDate" && field.NewValue is null);
        Assert.Contains(parkChange.Fields, field => field.Field == "openingDateText" && field.NewValue == "1987");
        ParkGraphUpsertChange itemChange = Assert.Single(result.Value.Changes, change => change.EntityType == "ParkItem");
        Assert.Contains(itemChange.Fields, field => field.Field == "attractionDetails.openingDate" && field.NewValue is null);
        Assert.Contains(itemChange.Fields, field => field.Field == "attractionDetails.openingDateText" && field.NewValue == "1988");

        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

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
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(),
            MeasurementConversionService.Instance);

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

    [Fact]
    public async Task ApplyAsync_WhenOnlyItemDescriptionChanges_ShouldDetectAndUpdateContent()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Wakala",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Ancienne description"),
            },
        };

        ParkItem? updatedItem = null;
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
            .ReturnsAsync(new[] { item });
        parkItemRepository
            .Setup(value => value.UpdateAsync("item-1", It.IsAny<ParkItem>(), It.IsAny<CancellationToken>()))
            .Callback<string, ParkItem, CancellationToken>((_, value, _) => updatedItem = value)
            .ReturnsAsync((string _, ParkItem value, CancellationToken _) => value);

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
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "items": [
            {
              "id": "item-1",
              "name": "Wakala",
              "descriptions": [
                {
                  "languageCode": "fr",
                  "value": "Nouvelle description"
                }
              ]
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
        ParkGraphUpsertChange itemChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkItem");
        Assert.Equal("Updated", itemChange.ChangeType);
        ParkGraphUpsertFieldChange fieldChange = Assert.Single(itemChange.Fields, field => field.Field == "descriptions.fr");
        Assert.Equal("Ancienne description", fieldChange.OldValue);
        Assert.Equal("Nouvelle description", fieldChange.NewValue);
        Assert.NotNull(updatedItem);
        Assert.Contains(updatedItem.Descriptions, description => description.LanguageCode == "fr" && description.Value == "Nouvelle description");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenImageIsUnchanged_ShouldNotUpdateImageMetadata()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image image = new Image
        {
            Id = "image-1",
            OriginalFileName = "image.jpg",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsPublished = true,
            IsCurrent = false,
            Description = "Image description",
            AltTexts = new List<LocalizedText>
            {
                new LocalizedText("fr", "Texte alternatif"),
            },
            Captions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Legende"),
            },
            Credits = new List<LocalizedText>
            {
                new LocalizedText("fr", "Credit"),
            },
            TagIds = new List<string>
            {
                "tag-1",
            },
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "imageId": "image-1",
              "ownerType": "Park",
              "ownerId": "park-1",
              "category": "Park",
              "isPublished": true,
              "setAsCurrent": false,
              "description": "Image description",
              "altTexts": [
                {
                  "languageCode": "fr",
                  "value": "Texte alternatif"
                }
              ],
              "captions": [
                {
                  "languageCode": "fr",
                  "value": "Legende"
                }
              ],
              "credits": [
                {
                  "languageCode": "fr",
                  "value": "Credit"
                }
              ],
              "tagIds": [
                "tag-1"
              ]
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
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Image");
        Assert.Equal("Unchanged", imageChange.ChangeType);
        Assert.Empty(imageChange.Fields);
        Assert.Equal(0, result.Value.Counts.Updated);
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenSupprContainsImageId_ShouldReportDeletedChangeWithoutDeleting()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image image = new Image
        {
            Id = "image-1",
            OriginalFileName = "duplicate.jpg",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "suppr": [
            "image-1"
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Image");
        Assert.Equal("Deleted", imageChange.ChangeType);
        Assert.Equal("image-1", imageChange.EntityId);
        Assert.Equal(1, result.Value.Counts.Deleted);
        Assert.True(result.Value.CanApply);
        imageRepository.Verify(value => value.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenSupprContainsParkItem_ShouldDeleteItemAndSearchProjection()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Duplicate item",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.Attraction,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        parkItemRepository
            .Setup(value => value.DeleteAsync("item-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.DeleteAsync(SearchProjectionResourceTypes.ParkItems, "item-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(
                It.Is<PublicSeoUpdate>(update => update.PreviousParkItems.Any(previousItem => previousItem.Id == "item-1")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "suppr": [
            {
              "entityType": "ParkItem",
              "id": "item-1"
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
        ParkGraphUpsertChange itemChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkItem");
        Assert.Equal("Deleted", itemChange.ChangeType);
        Assert.Equal("item-1", itemChange.EntityId);
        Assert.Equal(1, result.Value.Counts.Deleted);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenRemoteImageSourceIsProvided_ShouldExposePreviewWithoutImporting()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://cdn.example.test/logo.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/logo.webp",
              "ownerKey": "park",
              "category": "Logo",
              "withWatermark": true
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Image");
        Assert.Equal("Created", imageChange.ChangeType);
        Assert.Contains(imageChange.Fields, field => field.Field == "sourceUrl" && field.NewValue == "https://cdn.example.test/logo.webp");
        Assert.Contains(imageChange.Fields, field => field.Field == "ownerType" && field.NewValue == "Park");
        Assert.Contains(imageChange.Fields, field => field.Field == "category" && field.NewValue == "Logo");
        Assert.Contains(imageChange.Fields, field => field.Field == "withWatermark" && field.NewValue == "false");
        remoteImageImporter.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenRemoteImageSourceAlreadyExistsForOwner_ShouldReportSkippedWarning()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image existingImage = new Image
        {
            Id = "image-existing-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            SourceUrl = "https://cdn.example.test/photo.webp",
            IsPublished = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://cdn.example.test/photo.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingImage);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/photo.webp",
              "ownerKey": "park",
              "category": "Park"
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.Contains(result.Value.Warnings, warning => warning.Contains("sourceUrl already exists", StringComparison.Ordinal) && warning.Contains("image-existing-1", StringComparison.Ordinal));
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value.Changes, change => change.EntityType == "Image");
        Assert.Equal("Skipped", imageChange.ChangeType);
        Assert.Contains(imageChange.Fields, field => field.Field == "duplicateImageId" && field.NewValue == "image-existing-1");
        remoteImageImporter.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenManufacturerReferenceHasNoParkContext_ShouldApplyReferenceOnlyDocument()
    {
        AttractionManufacturer? createdManufacturer = null;
        Mock<IAttractionManufacturerRepository> attractionManufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        attractionManufacturerRepository
            .Setup(value => value.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AttractionManufacturer>());
        attractionManufacturerRepository
            .Setup(value => value.CreateAsync(It.IsAny<AttractionManufacturer>(), It.IsAny<CancellationToken>()))
            .Callback<AttractionManufacturer, CancellationToken>((manufacturer, _) =>
            {
                manufacturer.Id = "manufacturer-1";
                createdManufacturer = manufacturer;
            })
            .ReturnsAsync((AttractionManufacturer manufacturer, CancellationToken _) => manufacturer);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            attractionManufacturerRepository.Object,
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "references": {
            "manufacturers": [
              {
                "key": "manufacturer:mack-rides",
                "name": "Mack Rides",
                "isVisible": false,
                "adminReviewStatus": "Validated"
              }
            ]
          }
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = null,
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.TargetParkId);
        Assert.True(result.Value.CanApply);
        Assert.NotNull(createdManufacturer);
        Assert.Equal("Mack Rides", createdManufacturer.Name);
        Assert.False(createdManufacturer.IsVisible);
        attractionManufacturerRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenRemoteManufacturerImageHasNoParkContext_ShouldImportAndSetManufacturerLogo()
    {
        AttractionManufacturer manufacturer = new AttractionManufacturer
        {
            Id = "manufacturer-1",
            Name = "Mack Rides",
        };

        Mock<IAttractionManufacturerRepository> attractionManufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        attractionManufacturerRepository
            .Setup(value => value.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { manufacturer });
        attractionManufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manufacturer);
        attractionManufacturerRepository
            .Setup(value => value.UpdateAsync(
                "manufacturer-1",
                It.Is<AttractionManufacturer>(value => value.CurrentLogoImageId == "image-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, AttractionManufacturer value, CancellationToken _) => value);

        Image importedImage = new Image
        {
            Id = "image-1",
            OwnerType = ImageOwnerType.AttractionManufacturer,
            OwnerId = "manufacturer-1",
            Category = ImageCategory.Logo,
            SourceUrl = "https://cdn.example.test/logo.png",
        };
        Image currentImage = new Image
        {
            Id = "image-1",
            OwnerType = ImageOwnerType.AttractionManufacturer,
            OwnerId = "manufacturer-1",
            Category = ImageCategory.Logo,
            SourceUrl = "https://cdn.example.test/logo.png",
            IsCurrent = true,
        };

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        remoteImageImporter
            .Setup(value => value.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.OwnerType == ImageOwnerType.AttractionManufacturer
                    && request.OwnerId == "manufacturer-1"
                    && request.Category == ImageCategory.Logo
                    && request.WithWatermark == false
                    && request.SetAsCurrent == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-1", "https://cdn.example.test/logo.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        imageRepository
            .Setup(value => value.SetCurrentAsync("image-1", ImageOwnerType.AttractionManufacturer, "manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);
        imageRepository
            .Setup(value => value.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-1", ImageCategory.Logo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            attractionManufacturerRepository.Object,
            imageRepository.Object,
            remoteImageImporter.Object,
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "references": {
            "manufacturers": [
              {
                "key": "manufacturer:mack-rides",
                "id": "manufacturer-1",
                "name": "Mack Rides"
              }
            ]
          },
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/logo.png",
              "ownerKey": "manufacturer:mack-rides",
              "category": "Logo",
              "setAsCurrent": true,
              "withWatermark": false
            }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = null,
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("image-1", manufacturer.CurrentLogoImageId);
        Assert.Contains(result.Value!.Changes, change => change.EntityType == "Image" && change.ChangeType == "Created");
        attractionManufacturerRepository.VerifyAll();
        remoteImageImporter.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.Verify(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-1", It.IsAny<CancellationToken>()), Times.Once);
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenRemoteLogoIsProvided_ShouldImportAndSetCurrent()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image importedImage = new Image
        {
            Id = "image-remote-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Logo,
            SourceUrl = "https://cdn.example.test/logo.webp",
            IsPublished = true,
        };

        Image currentImage = new Image
        {
            Id = "image-remote-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Logo,
            SourceUrl = "https://cdn.example.test/logo.webp",
            IsPublished = true,
            IsCurrent = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, Park value, CancellationToken _) => value);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://cdn.example.test/logo.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        imageRepository
            .Setup(value => value.SetCurrentAsync("image-remote-1", ImageOwnerType.Park, "park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);
        imageRepository
            .Setup(value => value.GetCurrentByOwnerAsync(ImageOwnerType.Park, "park-1", ImageCategory.Logo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        remoteImageImporter
            .Setup(value => value.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.SourceUrl == "https://cdn.example.test/logo.webp"
                    && request.OwnerType == ImageOwnerType.Park
                    && request.OwnerId == "park-1"
                    && request.Category == ImageCategory.Logo
                    && request.WithWatermark == false
                    && request.SetAsCurrent == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/logo.webp",
              "ownerKey": "park",
              "category": "Logo"
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
        Assert.Equal("image-remote-1", park.CurrentLogoImageId);
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Image");
        Assert.Equal("Created", imageChange.ChangeType);
        Assert.Equal("image-remote-1", imageChange.EntityId);
        Assert.Contains(imageChange.Fields, field => field.Field == "imageId" && field.NewValue == "image-remote-1");
        Assert.Contains(imageChange.Fields, field => field.Field == "internalUrl" && field.NewValue == "/images/image-remote-1");
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        remoteImageImporter.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenRemoteImageSourceAlreadyExistsForOwner_ShouldSkipWithWarning()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image existingImage = new Image
        {
            Id = "image-existing-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            SourceUrl = "https://cdn.example.test/photo.webp",
            IsPublished = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://cdn.example.test/photo.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingImage);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/photo.webp",
              "ownerKey": "park",
              "category": "Park",
              "withWatermark": true
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
        Assert.True(result.Value!.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.Contains(result.Value.Warnings, warning => warning.Contains("sourceUrl already exists", StringComparison.Ordinal) && warning.Contains("image-existing-1", StringComparison.Ordinal));
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value.Changes, change => change.EntityType == "Image");
        Assert.Equal("Skipped", imageChange.ChangeType);
        Assert.Contains(imageChange.Fields, field => field.Field == "duplicateImageId" && field.NewValue == "image-existing-1");
        remoteImageImporter.Verify(value => value.ImportAsync(It.IsAny<RemoteImageImportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenRemoteParkImageRequestsWatermark_ShouldForwardWatermark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "FR",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        Image importedImage = new Image
        {
            Id = "image-remote-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            SourceUrl = "https://cdn.example.test/photo.webp",
            IsPublished = true,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAndSourceUrlAsync(ImageOwnerType.Park, "park-1", "https://cdn.example.test/photo.webp", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        remoteImageImporter
            .Setup(value => value.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.SourceUrl == "https://cdn.example.test/photo.webp"
                    && request.OwnerType == ImageOwnerType.Park
                    && request.OwnerId == "park-1"
                    && request.Category == ImageCategory.Park
                    && request.WithWatermark
                    && request.SetAsCurrent == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            remoteImageImporter.Object,
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "images": [
            {
              "sourceUrl": "https://cdn.example.test/photo.webp",
              "ownerKey": "park",
              "category": "Park",
              "withWatermark": true
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
        ParkGraphUpsertChange imageChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "Image");
        Assert.Equal("Created", imageChange.ChangeType);
        Assert.Contains(imageChange.Fields, field => field.Field == "withWatermark" && field.NewValue == "true");
        parkRepository.VerifyAll();
        imageRepository.VerifyAll();
        remoteImageImporter.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenImperialAttractionMeasurementsAreProvided_ShouldPersistMetricTruth()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Park",
            CountryCode = "US",
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
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "items": [
            {
              "key": "coaster-1",
              "name": "American Coaster",
              "category": "Attraction",
              "type": "RollerCoaster",
              "attractionDetails": {
                "heightInFeet": 200,
                "lengthInFeet": 5000,
                "speedInMph": 75,
                "dropInFeet": 180,
                "accessConditions": [
                  {
                    "type": "MinHeight",
                    "value": 48,
                    "unit": "Inch"
                  }
                ]
              }
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
        Assert.NotNull(createdItem.AttractionDetails);
        Assert.Equal(60.96d, createdItem.AttractionDetails.HeightInMeters);
        Assert.Equal(1524d, createdItem.AttractionDetails.LengthInMeters);
        Assert.Equal(120.7d, createdItem.AttractionDetails.SpeedInKmH);
        Assert.Equal(54.86d, createdItem.AttractionDetails.DropInMeters);
        AttractionAccessCondition condition = Assert.Single(createdItem.AttractionDetails.AccessConditions);
        Assert.Equal(121.92d, condition.Value);
        Assert.Equal(AttractionAccessConditionUnit.Centimeter, condition.Unit);
        Assert.Contains(result.Value!.Changes.SelectMany(change => change.Fields), field => field.Field == "attractionDetails.heightInMeters" && field.NewValue == "60.96");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenManufacturerMergeHasNoParkTarget_ShouldReturnAutonomousDiff()
    {
        AttractionManufacturer source = new AttractionManufacturer
        {
            Id = "manufacturer-source",
            Name = "Source Builder",
            LegalName = "Source Builder Ltd",
        };
        AttractionManufacturer target = new AttractionManufacturer
        {
            Id = "manufacturer-target",
            Name = "Target Builder",
            LegalName = "Target Builder SA",
        };

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-target", It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByManufacturerIdAsync("manufacturer-source", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-source", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            manufacturerRepository.Object,
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "merges": [
            {
              "entityType": "AttractionManufacturer",
              "sourceId": "manufacturer-source",
              "targetId": "manufacturer-target",
              "sections": {
                "identity": "source"
              }
            }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.CanApply);
        Assert.Null(result.Value.TargetParkId);
        ParkGraphUpsertChange targetChange = Assert.Single(result.Value.Changes, change => change.EntityId == "manufacturer-target");
        Assert.Equal("Updated", targetChange.ChangeType);
        Assert.Contains(targetChange.Fields, field => field.Field == "name" && field.OldValue == "Target Builder" && field.NewValue == "Source Builder");
        ParkGraphUpsertChange sourceChange = Assert.Single(result.Value.Changes, change => change.EntityId == "manufacturer-source");
        Assert.Equal("Deleted", sourceChange.ChangeType);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenManufacturerMergeIsApplied_ShouldMoveAttractionsAndDeleteSource()
    {
        AttractionManufacturer source = new AttractionManufacturer
        {
            Id = "manufacturer-source",
            Name = "Source Builder",
            LegalName = "Source Builder Ltd",
        };
        AttractionManufacturer target = new AttractionManufacturer
        {
            Id = "manufacturer-target",
            Name = "Target Builder",
            LegalName = "Target Builder SA",
        };
        ParkItem sourceItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Source Coaster",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            AttractionDetails = new AttractionDetails
            {
                ManufacturerId = "manufacturer-source",
            },
        };
        ParkItem? updatedItem = null;
        AttractionManufacturer? updatedManufacturer = null;

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-target", It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        manufacturerRepository
            .Setup(value => value.UpdateAsync("manufacturer-target", It.IsAny<AttractionManufacturer>(), It.IsAny<CancellationToken>()))
            .Callback<string, AttractionManufacturer, CancellationToken>((_, value, _) => updatedManufacturer = value)
            .ReturnsAsync((string _, AttractionManufacturer value, CancellationToken _) => value);
        manufacturerRepository
            .Setup(value => value.DeleteAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByManufacturerIdAsync("manufacturer-source", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { sourceItem });
        parkItemRepository
            .Setup(value => value.UpdateAsync("item-1", It.IsAny<ParkItem>(), It.IsAny<CancellationToken>()))
            .Callback<string, ParkItem, CancellationToken>((_, value, _) => updatedItem = value)
            .ReturnsAsync((string _, ParkItem value, CancellationToken _) => value);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-source", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.DeleteAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-source", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-target", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(value => value.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(It.Is<PublicSeoUpdate>(update => update.PreviousParkItems.Count == 1 && update.CurrentParkItems.Count == 1), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            manufacturerRepository.Object,
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "merges": [
            {
              "entityType": "manufacturer",
              "sourceId": "manufacturer-source",
              "targetId": "manufacturer-target",
              "sections": {
                "identity": "source"
              }
            }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(updatedManufacturer);
        Assert.Equal("Source Builder", updatedManufacturer.Name);
        Assert.NotNull(updatedItem);
        Assert.Equal("manufacturer-target", updatedItem.AttractionDetails?.ManufacturerId);
        Assert.Contains(result.Value!.Changes, change => change.EntityId == "manufacturer-source" && change.ChangeType == "Deleted");
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenManufacturerMergeCopiesSourceLogo_ShouldMoveLogoBeforeUpdatingTarget()
    {
        AttractionManufacturer source = new AttractionManufacturer
        {
            Id = "manufacturer-source",
            Name = "Anton Schwarzkopf",
            CurrentLogoImageId = "logo-source",
        };
        AttractionManufacturer target = new AttractionManufacturer
        {
            Id = "manufacturer-target",
            Name = "Schwarzkopf",
            CurrentLogoImageId = "logo-target",
        };
        Image sourceLogo = new Image
        {
            Id = "logo-source",
            OwnerType = ImageOwnerType.AttractionManufacturer,
            OwnerId = "manufacturer-source",
            Category = ImageCategory.Logo,
            IsCurrent = true,
        };
        bool sourceLogoWasMovedBeforeManufacturerUpdate = false;
        AttractionManufacturer? updatedManufacturer = null;

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        manufacturerRepository
            .Setup(value => value.GetByIdAsync("manufacturer-target", It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        manufacturerRepository
            .Setup(value => value.UpdateAsync("manufacturer-target", It.IsAny<AttractionManufacturer>(), It.IsAny<CancellationToken>()))
            .Callback<string, AttractionManufacturer, CancellationToken>((_, value, _) =>
            {
                Assert.True(sourceLogoWasMovedBeforeManufacturerUpdate);
                updatedManufacturer = value;
            })
            .ReturnsAsync((string _, AttractionManufacturer value, CancellationToken _) => value);
        manufacturerRepository
            .Setup(value => value.DeleteAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(value => value.GetByManufacturerIdAsync("manufacturer-source", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-source", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { sourceLogo });
        imageRepository
            .Setup(value => value.SetCurrentAsync("logo-source", ImageOwnerType.AttractionManufacturer, "manufacturer-target", It.IsAny<CancellationToken>()))
            .Callback<string, ImageOwnerType, string, CancellationToken>((_, _, _, _) => sourceLogoWasMovedBeforeManufacturerUpdate = true)
            .ReturnsAsync(sourceLogo);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.DeleteAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-source", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-target", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            manufacturerRepository.Object,
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "merges": [
            {
              "entityType": "AttractionManufacturer",
              "sourceId": "manufacturer-source",
              "targetId": "manufacturer-target",
              "sections": {
                "identity": "source",
                "logo": "source"
              }
            }
          ]
        }
        """);

        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(sourceLogoWasMovedBeforeManufacturerUpdate);
        Assert.NotNull(updatedManufacturer);
        Assert.Equal("Anton Schwarzkopf", updatedManufacturer.Name);
        Assert.Equal("logo-source", updatedManufacturer.CurrentLogoImageId);
        ParkGraphUpsertChange targetChange = Assert.Single(result.Value!.Changes, change => change.EntityId == "manufacturer-target");
        Assert.Contains(targetChange.Fields, field => field.Field == "currentLogoImageId" && field.OldValue == "logo-target" && field.NewValue == "logo-source");
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenOpeningHoursAreInGlobalJson_ShouldReportScheduleChangeWithoutSaving()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Schedule Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(value => value.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder());

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "openingHours": {
            "parkId": "park-1",
            "timeZoneId": "Europe/Paris",
            "sourceUrl": "https://example.test/hours",
            "regularRules": [
              {
                "id": "summer",
                "startDate": "2026-07-01",
                "endDate": "2026-07-31",
                "daysOfWeek": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
                "isClosed": false,
                "sortOrder": 1,
                "timeRanges": [
                  {
                    "opensAt": "10:00",
                    "closesAt": "18:00",
                    "closesNextDay": false,
                    "lastAdmissionAt": "17:30",
                    "lastAdmissionNextDay": false
                  }
                ]
              }
            ],
            "dateOverrides": []
          }
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkGraphUpsertChange openingHoursChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkOpeningHours");
        Assert.Equal("Created", openingHoursChange.ChangeType);
        Assert.Contains(openingHoursChange.Fields, field => field.Field == "openingHours.timeZoneId" && field.NewValue == "Europe/Paris");
        Assert.Contains(openingHoursChange.Fields, field => field.Field == "openingHours.regularRules");
        Assert.True(result.Value.CanApply);
        openingHoursRepository.Verify(value => value.UpsertAsync(It.IsAny<ParkOpeningHoursSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenOpeningHoursInGlobalJsonAreEmpty_ShouldSkipScheduleWithoutErrors()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Schedule Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder());

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "openingHours": {
            "parkId": "",
            "timeZoneId": "Europe/Paris",
            "sourceUrl": "",
            "notes": "",
            "lastVerifiedAtUtc": "",
            "regularRules": [],
            "dateOverrides": []
          }
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Errors);
        Assert.DoesNotContain(result.Value.Changes, change => change.EntityType == "ParkOpeningHours");
        openingHoursRepository.Verify(value => value.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        openingHoursRepository.Verify(value => value.UpsertAsync(It.IsAny<ParkOpeningHoursSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenOpeningHoursAreInGlobalJson_ShouldSaveScheduleWithoutRefreshingSitemap()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Schedule Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        ParkOpeningHoursSchedule? savedSchedule = null;

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(value => value.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(value => value.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkOpeningHoursSchedule?)null);
        openingHoursRepository
            .Setup(value => value.UpsertAsync(It.IsAny<ParkOpeningHoursSchedule>(), It.IsAny<CancellationToken>()))
            .Callback<ParkOpeningHoursSchedule, CancellationToken>((schedule, _) => savedSchedule = schedule)
            .ReturnsAsync((ParkOpeningHoursSchedule schedule, CancellationToken _) => schedule);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(value => value.NotifyAsync(
                It.Is<PublicSeoUpdate>(update =>
                    !update.SuppressSitemapRefresh
                    && update.CurrentParks.Any(currentPark => currentPark.Id == "park-1")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder());

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "openingHours": {
            "parkId": "park-1",
            "timeZoneId": "Europe/Paris",
            "sourceUrl": "https://example.test/hours",
            "regularRules": [
              {
                "id": "summer",
                "startDate": "2026-07-01",
                "endDate": "2026-07-31",
                "daysOfWeek": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
                "isClosed": false,
                "sortOrder": 1,
                "timeRanges": [
                  {
                    "opensAt": "10:00",
                    "closesAt": "18:00",
                    "closesNextDay": false,
                    "lastAdmissionAt": "17:30",
                    "lastAdmissionNextDay": false
                  }
                ]
              }
            ],
            "dateOverrides": [
              {
                "localDate": "2026-07-14",
                "isClosed": true,
                "reasons": [
                  { "languageCode": "fr", "value": "Privatisation" }
                ],
                "timeRanges": []
              }
            ]
          }
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
        Assert.NotNull(savedSchedule);
        Assert.Equal("park-1", savedSchedule.ParkId);
        Assert.Equal("Europe/Paris", savedSchedule.TimeZoneId);
        Assert.Single(savedSchedule.RegularRules);
        Assert.Single(savedSchedule.DateOverrides);
        Assert.Contains(savedSchedule.DateOverrides[0].Reasons, static reason => reason.LanguageCode == "fr" && reason.Value == "Privatisation");
        Assert.NotEmpty(savedSchedule.CoverageSegments);
        ParkGraphUpsertChange openingHoursChange = Assert.Single(result.Value!.Changes, change => change.EntityType == "ParkOpeningHours");
        Assert.Equal("Created", openingHoursChange.ChangeType);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WhenOpeningHoursUseLegacyLocalizedFields_ShouldRejectImport()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Schedule Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance,
            openingHoursRepository.Object,
            new ParkOpeningHoursScheduleNormalizer(),
            new ParkOpeningHoursCoverageSegmentBuilder());

        using JsonDocument document = JsonDocument.Parse("""
        {
          "mode": "merge",
          "openingHours": {
            "parkId": "park-1",
            "timeZoneId": "Europe/Paris",
            "regularRules": [
              {
                "id": "summer",
                "startDate": "2026-07-01",
                "endDate": "2026-07-31",
                "daysOfWeek": ["Saturday"],
                "isClosed": false,
                "label": "Ouverture estivale",
                "timeRanges": [
                  {
                    "opensAt": "10:00",
                    "closesAt": "18:00"
                  }
                ]
              }
            ],
            "dateOverrides": [
              {
                "localDate": "2026-07-14",
                "isClosed": true,
                "reason": "Privatisation",
                "timeRanges": []
              }
            ]
          }
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

        ApplicationResult<ParkGraphUpsertResult> result = await processor.PreviewAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("openingHours.regularRules[0].label", StringComparison.Ordinal));
        Assert.Contains(result.Value.Errors, static error => error.Contains("openingHours.dateOverrides[0].reason", StringComparison.Ordinal));
        openingHoursRepository.Verify(value => value.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        openingHoursRepository.Verify(value => value.UpsertAsync(It.IsAny<ParkOpeningHoursSchedule>(), It.IsAny<CancellationToken>()), Times.Never);
        parkRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        historyRepository.VerifyAll();
    }
}
