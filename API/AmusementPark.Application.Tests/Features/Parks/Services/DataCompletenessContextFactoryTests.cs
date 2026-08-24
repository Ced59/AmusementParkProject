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
            CurrentLogoImageId = "park-image-1",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            Descriptions = new List<LocalizedText>
            {
                new("fr", "Projection Park appartient à l’univers de Discoveryland et à l’identité du parc parisien. La page publique confirme l'inventaire actuel."),
            },
        };
        ParkItem parkItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Projection Ride",
            IsVisible = true,
            Descriptions = new List<LocalizedText>
            {
                new("fr", "Projection Ride appartient à l’univers de Discoveryland et à l’identité du parc parisien. La page publique confirme l'inventaire actuel."),
            },
        };
        Image parkImage = new Image
        {
            Id = "park-image-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Logo,
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
                null,
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
        Assert.False(current.HasPublishedCurrentLogo);
        Assert.Equal(0, current.ParkItemPublishedImageCount);
        Assert.Equal(0, current.PublishedArticleCount);
        Assert.False(current.HasPublicSeoSignals);
        Assert.True(current.HasDocumentedRemainingDebt);
        Assert.False(current.HasNoForbiddenPublicText);
        Assert.False(current.HasStructuredTechnicalDataOnly);
        Assert.False(current.HasNoFormulaicPublicText);
        Assert.True(projected.ProjectForPublication);
        Assert.Equal(1, projected.ParkPublishedImageCount);
        Assert.True(projected.HasPublishedCurrentLogo);
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
        Assert.False(projected.HasNoFormulaicPublicText);

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

    [Fact]
    public async Task BuildParkContextsAsync_ShouldIgnoreHiddenTimelinesAndHiddenItemMediaInPublicTextAudit()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Public Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            Descriptions = new List<LocalizedText> { new("fr", "Un parc familial coloré et ancré dans sa ville.") },
        };
        ParkItem visibleItem = new ParkItem
        {
            Id = "visible-item",
            ParkId = "park-1",
            Name = "Visible Ride",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = new List<LocalizedText> { new("fr", "Une attraction familiale entourée de palmiers.") },
        };
        ParkItem hiddenItem = new ParkItem
        {
            Id = "hidden-item",
            ParkId = "park-1",
            Name = "Hidden Ride",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.NotRelevant,
            Descriptions = new List<LocalizedText> { new("fr", "La page publique confirme l'inventaire actuel.") },
        };
        Image hiddenItemImage = new Image
        {
            Id = "hidden-image",
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = "hidden-item",
            Category = ImageCategory.ParkItem,
            IsPublished = true,
            AltTexts = new List<LocalizedText> { new("fr", "Image d'audit de la base de données.") },
        };
        HistoryEvent hiddenParkHistory = new HistoryEvent
        {
            Id = "hidden-park-history",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            IsVisible = false,
            Titles = new List<LocalizedText> { new("fr", "Présence publique confirmée") },
            Article = new HistoryArticle
            {
                IsPublished = false,
                Summaries = new List<LocalizedText> { new("fr", "Ces sources décrivent l'inventaire actuel.") },
            },
        };
        HistoryEvent hiddenItemHistory = new HistoryEvent
        {
            Id = "hidden-item-history",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "hidden-item",
            IsVisible = true,
            Summaries = new List<LocalizedText> { new("fr", "Une visite indépendante confirme l'inventaire actuel.") },
        };

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetCountsByCategoryForParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>
            {
                ["park-1"] = new Dictionary<ParkItemCategory, int> { [ParkItemCategory.Attraction] = 2 },
            });
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleItem, hiddenItem });

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "visible-item", "hidden-item" })),
                ImageCategory.ParkItem,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenItemImage });

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(repository => repository.GetOwnerTimelinesAsync(
                HistoryEntityType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenParkHistory });
        historyEventRepository
            .Setup(repository => repository.GetOwnerTimelinesAsync(
                HistoryEntityType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "visible-item", "hidden-item" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { hiddenItemHistory });

        IReadOnlyDictionary<string, ParkItemVisibilityCounts> visibilityCounts =
            new Dictionary<string, ParkItemVisibilityCounts>
            {
                ["park-1"] = new ParkItemVisibilityCounts { TotalCount = 2, VisibleCount = 1 },
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

        Assert.True(currentContexts["park-1"].HasNoForbiddenPublicText);
        Assert.True(projectedContexts["park-1"].HasNoForbiddenPublicText);
        Assert.True(currentContexts["park-1"].HasNoFormulaicPublicText);
        Assert.True(projectedContexts["park-1"].HasNoFormulaicPublicText);
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }
}
