using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
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
        Assert.Equal(0, projected.ParkPublishedImageCount);
        Assert.True(projected.HasPublishedCurrentLogo);
        Assert.Equal(0, projected.ParkImagesWithResolvedOwnerCount);
        Assert.Equal(0, projected.ParkImagesWithLocalizedAltTextCount);
        Assert.Equal(1, projected.ParkItemPublishedImageCount);
        Assert.False(projected.HasOriginalMedia);
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
        List<Image> parkImages = new List<Image>();
        List<HistoryEvent> parkHistoryEvents = new List<HistoryEvent> { hiddenParkHistory };

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
            .ReturnsAsync(parkImages);
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
            .ReturnsAsync(parkHistoryEvents);
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

        visibleItem.AttractionDetails = new AttractionDetails
        {
            Model = "SLC 689m Standard",
        };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> structuredMetricContexts =
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

        Assert.True(structuredMetricContexts["park-1"].HasNoForbiddenPublicText);

        visibleItem.AttractionDetails = new AttractionDetails
        {
            AccessConditions = new List<AttractionAccessCondition>
            {
                new()
                {
                    Type = AttractionAccessConditionType.Custom,
                    CustomTypeLabel = new List<LocalizedText> { new("en", "Wheelchair &amp; transfer") },
                },
            },
        };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedAccessConditionContexts =
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

        Assert.False(encodedAccessConditionContexts["park-1"].HasNoForbiddenPublicText);

        visibleItem.AttractionDetails = null;
        parkImages.Add(new Image
        {
            Id = "park-image",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsPublished = true,
            Credits = new List<LocalizedText> { new("en", "Rock &amp; Roll Photography") },
        });
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedImageCreditContexts =
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

        Assert.False(encodedImageCreditContexts["park-1"].HasNoForbiddenPublicText);

        parkImages.Clear();
        parkImages.Add(new Image
        {
            Id = "park-image",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsPublished = true,
            OriginalFileName = "Rock &amp; Roll.jpg",
        });
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedImageFileNameContexts =
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

        Assert.False(encodedImageFileNameContexts["park-1"].HasNoForbiddenPublicText);

        parkImages.Clear();
        parkHistoryEvents.Add(new HistoryEvent
        {
            Id = "visible-history",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            IsVisible = true,
            Sources = new List<HistorySourceReference>
            {
                new() { Label = "Rock &amp; Roll Archive", Url = "https://example.test/archive" },
            },
        });
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedSourceLabelContexts =
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

        Assert.False(encodedSourceLabelContexts["park-1"].HasNoForbiddenPublicText);

        parkHistoryEvents.RemoveAt(parkHistoryEvents.Count - 1);
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> encodedOpeningHours =
            new Dictionary<string, ParkOpeningHoursSchedule>
            {
                ["park-1"] = new ParkOpeningHoursSchedule
                {
                    ParkId = "park-1",
                    Notes = "Internal Rock &amp; Roll note",
                    RegularRules = new List<ParkOpeningHoursRule>
                    {
                        new()
                        {
                            StartDate = new DateOnly(2026, 1, 1),
                            EndDate = new DateOnly(2026, 12, 31),
                            Labels = new List<LocalizedText> { new("en", "Summer &amp; evenings") },
                        },
                    },
                },
            };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedOpeningHoursContexts =
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
                openingHoursSchedulesByParkId: encodedOpeningHours);

        Assert.False(encodedOpeningHoursContexts["park-1"].HasNoForbiddenPublicText);

        encodedOpeningHours["park-1"].RegularRules[0].Labels = new List<LocalizedText> { new("en", "Summer evenings") };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> internalOpeningHoursNoteContexts =
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
                openingHoursSchedulesByParkId: encodedOpeningHours);

        Assert.True(internalOpeningHoursNoteContexts["park-1"].HasNoForbiddenPublicText);

        IReadOnlyDictionary<string, AmusementPark.Core.Domain.Parks.ParkPricing> encodedPricing =
            new Dictionary<string, AmusementPark.Core.Domain.Parks.ParkPricing>
            {
                ["park-1"] = new AmusementPark.Core.Domain.Parks.ParkPricing
                {
                    ParkId = "park-1",
                    AdmissionOffers = new List<ParkAdmissionPriceOffer>
                    {
                        new()
                        {
                            Code = "adult",
                            Labels = new List<LocalizedText> { new("en", "Rock &amp; Roll admission") },
                        },
                    },
                },
            };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> encodedPricingContexts =
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
                pricingByParkId: encodedPricing);

        Assert.False(encodedPricingContexts["park-1"].HasNoForbiddenPublicText);

        park.Status = ParkStatus.Planned;
        IReadOnlyDictionary<string, ParkDataCompletenessContext> plannedParkContexts =
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
                openingHoursSchedulesByParkId: encodedOpeningHours,
                pricingByParkId: encodedPricing);

        Assert.True(plannedParkContexts["park-1"].HasNoForbiddenPublicText);
        park.Status = ParkStatus.Operating;

        IReadOnlyDictionary<string, AmusementPark.Core.Domain.Parks.ParkPricing> archivedPricing =
            new Dictionary<string, AmusementPark.Core.Domain.Parks.ParkPricing>
            {
                ["park-1"] = new AmusementPark.Core.Domain.Parks.ParkPricing
                {
                    ParkId = "park-1",
                    AdmissionOffers = new List<ParkAdmissionPriceOffer>
                    {
                        new()
                        {
                            Code = "expired",
                            ValidTo = new DateOnly(2000, 1, 1),
                            Labels = new List<LocalizedText> { new("en", "Rock &amp; Roll archived admission") },
                        },
                    },
                    HistoricalSnapshots = Enumerable.Range(0, 11)
                        .Select(index => new ParkPricingSnapshot
                        {
                            Year = 2026 - index,
                            Notes = index == 10
                                ? new List<LocalizedText> { new("en", "Rock &amp; Roll archived snapshot") }
                                : new List<LocalizedText>(),
                        })
                        .ToList(),
                },
            };
        IReadOnlyDictionary<string, ParkDataCompletenessContext> archivedPricingContexts =
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
                pricingByParkId: archivedPricing);

        Assert.True(archivedPricingContexts["park-1"].HasNoForbiddenPublicText);
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }

    [Fact]
    public async Task BuildParkContextsAsync_WhenReferencedPublicTextIsEncoded_FlagsEachSharedReferenceSurface()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Reference Park",
            FounderId = "founder-1",
            OperatorId = "operator-1",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        ParkItem parkItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Reference Ride",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails { ManufacturerId = "manufacturer-1" },
        };
        ParkFounder founder = new ParkFounder { Id = "founder-1", Name = "Clean founder" };
        ParkOperator parkOperator = new ParkOperator { Id = "operator-1", Name = "Clean operator" };
        AttractionManufacturer manufacturer = new AttractionManufacturer
        {
            Id = "manufacturer-1",
            Name = "Clean manufacturer",
            IsVisible = true,
        };
        Image referenceImage = new Image
        {
            Id = "reference-image",
            OwnerType = ImageOwnerType.AttractionManufacturer,
            OwnerId = "manufacturer-1",
            Category = ImageCategory.Manufacturer,
            IsPublished = true,
        };

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetCountsByCategoryForParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>());
        parkItemRepository
            .Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkItem });

        Mock<IParkFounderRepository> founderRepository = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        founderRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "founder-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { founder });
        Mock<IParkOperatorRepository> operatorRepository = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        operatorRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "operator-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkOperator });
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "manufacturer-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { manufacturer });

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.Park,
                It.IsAny<IReadOnlyCollection<string>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.ParkItem,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.ParkFounder,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.Founder,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.ParkOperator,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.Operator,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.AttractionManufacturer,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.Manufacturer,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { referenceImage });

        async Task<bool> HasNoForbiddenPublicTextAsync()
        {
            IReadOnlyDictionary<string, ParkDataCompletenessContext> contexts =
                await DataCompletenessContextFactory.BuildParkContextsAsync(
                    new[] { park },
                    new Dictionary<string, ParkItemVisibilityCounts>(),
                    new Dictionary<string, ParkOpeningHoursScheduleSummary>(),
                    new ParkOpeningHoursAdminStatusResolverAccessor(static _ => ParkOpeningHoursAdminStatus.NotConfigured),
                    parkItemRepository.Object,
                    null,
                    imageRepository.Object,
                    null,
                    CancellationToken.None,
                    parkFounderRepository: founderRepository.Object,
                    parkOperatorRepository: operatorRepository.Object,
                    attractionManufacturerRepository: manufacturerRepository.Object);
            return contexts["park-1"].HasNoForbiddenPublicText;
        }

        founder.Name = "Rock &amp; Roll founder";
        Assert.False(await HasNoForbiddenPublicTextAsync());
        founder.Name = "Clean founder";

        parkOperator.Description = new List<LocalizedText> { new("en", "<p>Rock &rsquo; Roll operator</p>") };
        Assert.False(await HasNoForbiddenPublicTextAsync());
        parkOperator.Description.Clear();

        manufacturer.LegalName = "Rock &amp; Roll manufacturer";
        Assert.False(await HasNoForbiddenPublicTextAsync());
        manufacturer.LegalName = null;

        referenceImage.Credits = new List<LocalizedText> { new("en", "Rock &amp; Roll Photography") };
        Assert.False(await HasNoForbiddenPublicTextAsync());
        referenceImage.Credits.Clear();

        parkOperator.ContactDetails = new ParkReferenceContactDetails { Email = "admin@example.com" };
        Assert.True(await HasNoForbiddenPublicTextAsync());
        parkItemRepository.VerifyAll();
        founderRepository.VerifyAll();
        operatorRepository.VerifyAll();
        manufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
    }
}
