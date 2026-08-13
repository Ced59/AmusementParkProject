using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Services;

public sealed class DataCompletenessContextFactoryTests
{
    [Fact]
    public async Task BuildParkContextsAsync_WhenPublicationIsProjected_IncludesAuditedDraftsAndValidationEffects()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Projection Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            Descriptions = new List<LocalizedText> { new("fr", "Une description éditoriale suffisamment développée pour le parc.") },
        };
        ParkItem parkItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Projection Ride",
            IsVisible = true,
            Descriptions = new List<LocalizedText>
            {
                new("fr", "La page publique confirme l'inventaire actuel de cette attraction."),
            },
        };
        Image parkImage = new Image
        {
            Id = "park-image-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsPublished = false,
            OriginalFileName = "park.jpg",
            AltTexts = new List<LocalizedText> { new("fr", "Vue générale du parc") },
        };
        Image itemImage = new Image
        {
            Id = "item-image-1",
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = "item-1",
            Category = ImageCategory.ParkItem,
            IsPublished = false,
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "history-1",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            Article = new HistoryArticle
            {
                IsPublished = false,
                Blocks = new List<HistoryArticleBlock>
                {
                    new() { Type = HistoryArticleBlockType.Paragraph },
                },
            },
        };

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetCountsByCategoryForParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>
            {
                ["park-1"] = new Dictionary<ParkItemCategory, int> { [ParkItemCategory.Attraction] = 1 },
            });
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkItem });

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                ImageCategory.Park,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkImage });
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                ImageCategory.ParkItem,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { itemImage });

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(repository => repository.GetOwnerTimelinesAsync(
                HistoryEntityType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });
        historyEventRepository
            .Setup(repository => repository.GetOwnerTimelinesAsync(
                HistoryEntityType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());

        IReadOnlyDictionary<string, ParkItemVisibilityCounts> visibilityCounts =
            new Dictionary<string, ParkItemVisibilityCounts>
            {
                ["park-1"] = new ParkItemVisibilityCounts { TotalCount = 1, VisibleCount = 1 },
            };
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> openingHours =
            new Dictionary<string, ParkOpeningHoursScheduleSummary>();

        IReadOnlyDictionary<string, ParkDataCompletenessContext> currentContexts =
            await DataCompletenessContextFactory.BuildParkContextsAsync(
                new[] { park },
                visibilityCounts,
                openingHours,
                new ParkOpeningHoursAdminStatusResolverAccessor(static _ => ParkOpeningHoursAdminStatus.NotConfigured),
                parkItemRepository.Object,
                null,
                imageRepository.Object,
                historyEventRepository.Object,
                CancellationToken.None);
        IReadOnlyDictionary<string, ParkDataCompletenessContext> projectedContexts =
            await DataCompletenessContextFactory.BuildParkContextsAsync(
                new[] { park },
                visibilityCounts,
                openingHours,
                new ParkOpeningHoursAdminStatusResolverAccessor(static _ => ParkOpeningHoursAdminStatus.NotConfigured),
                parkItemRepository.Object,
                null,
                imageRepository.Object,
                historyEventRepository.Object,
                CancellationToken.None,
                projectForPublication: true);

        ParkDataCompletenessContext current = currentContexts["park-1"];
        ParkDataCompletenessContext projected = projectedContexts["park-1"];
        Assert.False(current.ProjectForPublication);
        Assert.Equal(0, current.ParkPublishedImageCount);
        Assert.Equal(0, current.ParkItemPublishedImageCount);
        Assert.Equal(0, current.PublishedArticleCount);
        Assert.False(current.HasPublicSeoSignals);
        Assert.True(current.HasDocumentedRemainingDebt);
        Assert.False(current.HasNoForbiddenPublicText);
        Assert.False(current.HasStructuredTechnicalDataOnly);
        Assert.True(projected.ProjectForPublication);
        Assert.Equal(1, projected.ParkPublishedImageCount);
        Assert.Equal(1, projected.ParkImagesWithResolvedOwnerCount);
        Assert.Equal(1, projected.ParkImagesWithLocalizedAltTextCount);
        Assert.Equal(1, projected.ParkItemPublishedImageCount);
        Assert.True(projected.HasOriginalMedia);
        Assert.Equal(1, projected.PublishedArticleCount);
        Assert.Equal(1, projected.StructuredArticleCount);
        Assert.True(projected.HasPublicSeoSignals);
        Assert.False(projected.HasDocumentedRemainingDebt);
        Assert.False(projected.HasNoForbiddenPublicText);
        Assert.False(projected.HasStructuredTechnicalDataOnly);

        park.AdminReviewStatus = AdminReviewStatus.NotRelevant;
        IReadOnlyDictionary<string, ParkDataCompletenessContext> notRelevantContexts =
            await DataCompletenessContextFactory.BuildParkContextsAsync(
                new[] { park },
                visibilityCounts,
                openingHours,
                new ParkOpeningHoursAdminStatusResolverAccessor(static _ => ParkOpeningHoursAdminStatus.NotConfigured),
                parkItemRepository.Object,
                null,
                imageRepository.Object,
                historyEventRepository.Object,
                CancellationToken.None,
                projectForPublication: true);

        ParkDataCompletenessContext notRelevant = notRelevantContexts["park-1"];
        Assert.False(notRelevant.ProjectForPublication);
        Assert.Equal(0, notRelevant.ParkPublishedImageCount);
        Assert.Equal(0, notRelevant.ParkItemPublishedImageCount);
        Assert.Equal(0, notRelevant.PublishedArticleCount);
        Assert.False(notRelevant.HasPublicSeoSignals);
        Assert.True(notRelevant.HasDocumentedRemainingDebt);

        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }
}
