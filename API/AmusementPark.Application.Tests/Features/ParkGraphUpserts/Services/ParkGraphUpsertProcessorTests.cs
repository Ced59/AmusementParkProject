using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
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
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
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
              "category": "ParkLogo"
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
        Assert.Contains(imageChange.Fields, field => field.Field == "category" && field.NewValue == "ParkLogo");
        remoteImageImporter.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        historyRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenRemoteParkLogoIsProvided_ShouldImportAndSetCurrent()
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
            Category = ImageCategory.ParkLogo,
            SourceUrl = "https://cdn.example.test/logo.webp",
            IsPublished = true,
        };

        Image currentImage = new Image
        {
            Id = "image-remote-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.ParkLogo,
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
            .Setup(value => value.SetCurrentAsync("image-remote-1", ImageOwnerType.Park, "park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);

        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        remoteImageImporter
            .Setup(value => value.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.SourceUrl == "https://cdn.example.test/logo.webp"
                    && request.OwnerType == ImageOwnerType.Park
                    && request.OwnerId == "park-1"
                    && request.Category == ImageCategory.ParkLogo
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
              "category": "ParkLogo"
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
}
