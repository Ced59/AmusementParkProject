using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Services;

internal static class DataCompletenessContextFactory
{
    public static async Task<IReadOnlyDictionary<string, ParkDataCompletenessContext>> BuildParkContextsAsync(
        IReadOnlyCollection<Park> parks,
        IReadOnlyDictionary<string, ParkItemVisibilityCounts> visibilityCountsByParkId,
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> openingHoursSummariesByParkId,
        ParkOpeningHoursAdminStatusResolverAccessor openingHoursStatusResolver,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository? parkZoneRepository,
        IImageRepository? imageRepository,
        IHistoryEventRepository? historyEventRepository,
        CancellationToken cancellationToken,
        bool projectForPublication = false)
    {
        List<string> parkIds = parks
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (parkIds.Count == 0)
        {
            return new Dictionary<string, ParkDataCompletenessContext>(StringComparer.Ordinal);
        }

        IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>> categoryCountsByParkId =
            await parkItemRepository.GetCountsByCategoryForParkIdsAsync(parkIds, true, cancellationToken);
        IReadOnlyCollection<ParkItem> parkItems = await parkItemRepository.GetByParkIdsAsync(parkIds, true, cancellationToken);
        Dictionary<string, List<ParkItem>> parkItemsByParkId = parkItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkId))
            .GroupBy(static item => item.ParkId.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);

        Dictionary<string, List<ParkZone>> zonesByParkId = await LoadZonesByParkIdAsync(parkIds, parkZoneRepository, cancellationToken);
        Dictionary<string, List<Image>> parkImagesByParkId = await LoadImagesByOwnerIdAsync(ImageOwnerType.Park, parkIds, ImageCategory.Park, imageRepository, cancellationToken);
        Dictionary<string, List<Image>> parkItemImagesByItemId = await LoadImagesByOwnerIdAsync(
            ImageOwnerType.ParkItem,
            parkItems.Select(static item => item.Id ?? string.Empty).ToList(),
            ImageCategory.ParkItem,
            imageRepository,
            cancellationToken);
        Dictionary<string, List<HistoryEvent>> parkHistoryByParkId = await LoadHistoryByOwnerIdAsync(HistoryEntityType.Park, parkIds, historyEventRepository, cancellationToken);
        Dictionary<string, List<HistoryEvent>> parkItemHistoryByItemId = await LoadHistoryByOwnerIdAsync(
            HistoryEntityType.ParkItem,
            parkItems.Select(static item => item.Id ?? string.Empty).ToList(),
            historyEventRepository,
            cancellationToken);

        Dictionary<string, ParkDataCompletenessContext> contextsByParkId = new Dictionary<string, ParkDataCompletenessContext>(StringComparer.Ordinal);
        foreach (Park park in parks)
        {
            if (string.IsNullOrWhiteSpace(park.Id))
            {
                continue;
            }

            string parkId = park.Id.Trim();
            bool projectCurrentParkForPublication = projectForPublication
                && park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
            List<ParkItem> currentParkItems = parkItemsByParkId.GetValueOrDefault(parkId) ?? new List<ParkItem>();
            List<ParkZone> currentZones = zonesByParkId.GetValueOrDefault(parkId) ?? new List<ParkZone>();
            List<Image> currentParkImages = parkImagesByParkId.GetValueOrDefault(parkId) ?? new List<Image>();
            List<HistoryEvent> currentParkHistory = parkHistoryByParkId.GetValueOrDefault(parkId) ?? new List<HistoryEvent>();
            IReadOnlyDictionary<ParkItemCategory, int> currentCategoryCounts = categoryCountsByParkId.GetValueOrDefault(parkId)
                ?? new Dictionary<ParkItemCategory, int>();
            ParkItemVisibilityCounts? visibilityCounts = visibilityCountsByParkId.GetValueOrDefault(parkId);
            ParkOpeningHoursScheduleSummary? openingHoursSummary = openingHoursSummariesByParkId.GetValueOrDefault(parkId);
            IReadOnlyCollection<HistoryEvent> currentParkItemHistory = currentParkItems
                .SelectMany(item => string.IsNullOrWhiteSpace(item.Id)
                    ? Enumerable.Empty<HistoryEvent>()
                    : parkItemHistoryByItemId.GetValueOrDefault(item.Id.Trim()) ?? new List<HistoryEvent>())
                .ToList();
            IReadOnlyCollection<Image> currentParkItemImages = currentParkItems
                .SelectMany(item => string.IsNullOrWhiteSpace(item.Id)
                    ? Enumerable.Empty<Image>()
                    : parkItemImagesByItemId.GetValueOrDefault(item.Id.Trim()) ?? new List<Image>())
                .ToList();
            IReadOnlyCollection<Image> scoreParkImages = projectCurrentParkForPublication
                ? currentParkImages
                : currentParkImages.Where(static image => image.IsPublished).ToList();
            IReadOnlyCollection<Image> scoreParkItemImages = projectCurrentParkForPublication
                ? currentParkItemImages
                : currentParkItemImages.Where(static image => image.IsPublished).ToList();
            IReadOnlyCollection<HistoryEvent> scoreParkHistory = projectCurrentParkForPublication
                ? currentParkHistory
                : currentParkHistory.Where(static historyEvent => historyEvent.IsVisible).ToList();
            IReadOnlyCollection<HistoryEvent> scoreParkItemHistory = projectCurrentParkForPublication
                ? currentParkItemHistory
                : currentParkItemHistory.Where(static historyEvent => historyEvent.IsVisible).ToList();
            bool hasNoForbiddenPublicText = HasNoForbiddenParkRelatedPublicText(
                currentParkItems,
                currentZones,
                scoreParkImages,
                scoreParkItemImages,
                scoreParkHistory,
                scoreParkItemHistory,
                projectCurrentParkForPublication);

            ParkDataCompletenessContext context = new ParkDataCompletenessContext
            {
                ProjectForPublication = projectCurrentParkForPublication,
                ParkItemsTotalCount = visibilityCounts?.TotalCount ?? currentParkItems.Count,
                ParkItemsVisibleCount = visibilityCounts?.VisibleCount ?? currentParkItems.Count(static item => item.IsVisible),
                DistinctParkItemCategoryCount = currentCategoryCounts.Count(static count => count.Value > 0),
                ClosedImportantParkItemsCount = currentParkItems.Count(IsClosedImportantParkItem),
                ParkItemsWithKnownStatusOrDatesCount = currentParkItems.Count(HasKnownStatusOrDates),
                AttractionManufacturerIdsCount = currentParkItems.Count(HasManufacturer),
                AttractionsWithAccessConditionsCount = currentParkItems.Count(HasAccessConditions),
                HasOfficialZones = currentZones.Count > 0,
                ZonesTotalCount = currentZones.Count,
                ZonesWithDescriptionsCount = currentZones.Count(static zone => HasLocalizedText(zone.Names) || HasLocalizedText(zone.Descriptions)),
                ParkItemsAttachedToZonesCount = currentParkItems.Count(static item => !string.IsNullOrWhiteSpace(item.ZoneId)),
                ParkItemsWithDescriptionsCount = currentParkItems.Count(static item => HasLocalizedText(item.Descriptions)),
                CommercialOrServiceItemsWithDescriptionsCount = currentParkItems.Count(static item => IsCommercialOrService(item) && HasLocalizedText(item.Descriptions)),
                ParkPublishedImageCount = scoreParkImages.Count,
                ParkImagesWithResolvedOwnerCount = scoreParkImages.Count(image => string.Equals(image.OwnerId, parkId, StringComparison.Ordinal)),
                ParkImagesWithLocalizedAltTextCount = scoreParkImages.Count(static image => HasLocalizedText(image.AltTexts)),
                ParkItemPublishedImageCount = scoreParkItemImages.Count,
                HasOriginalMedia = scoreParkImages.Any(static image => !string.IsNullOrWhiteSpace(image.OriginalFileName)),
                HasOpeningHours = openingHoursSummary?.HasScheduleData == true,
                OpeningHoursStatus = openingHoursSummary is null
                    ? ParkOpeningHoursAdminStatus.NotConfigured
                    : openingHoursStatusResolver.ResolveStatus(openingHoursSummary),
                HasOpeningHoursSource = !string.IsNullOrWhiteSpace(openingHoursSummary?.SourceUrl),
                HasOpeningHoursTimeZone = !string.IsNullOrWhiteSpace(openingHoursSummary?.TimeZoneId),
                HasOpeningHoursExceptions = openingHoursSummary?.HasDateOverrides == true,
                HasOpeningHoursRecentVerification = openingHoursSummary?.LastVerifiedAtUtc >= DateTime.UtcNow.AddDays(-90),
                ParkHistoryEventCount = currentParkHistory.Count,
                MajorHistoryEventCount = currentParkHistory.Count(static historyEvent => historyEvent.IsMajor),
                ParkItemHistoryEventCount = currentParkItemHistory.Count,
                PublishedArticleCount = currentParkHistory.Count(historyEvent => HasPublishedArticle(historyEvent, projectCurrentParkForPublication)),
                StructuredArticleCount = currentParkHistory.Count(historyEvent => HasStructuredArticle(historyEvent, projectCurrentParkForPublication)),
                LocalizedHistoryContentCount = currentParkHistory.Count(HasLocalizedHistoryContent),
                HistoryEventsWithSourcesCount = currentParkHistory.Count(HasSources),
                HistoryEventsWithMediaCount = currentParkHistory.Count(HasMedia),
                ImportantReferencesWithDescriptionsCount = CountReferenceDescriptions(park, currentParkItems),
                ReferencesWithUsefulDetailsCount = CountReferenceDetails(park, currentParkItems),
                HasCriticalSources = HasCriticalSource(park, currentParkHistory),
                HasStructuredTechnicalDataOnly = hasNoForbiddenPublicText,
                HasNoForbiddenPublicText = hasNoForbiddenPublicText,
                HasDocumentedRemainingDebt = projectCurrentParkForPublication
                    ? false
                    : park.AdminReviewStatus != AdminReviewStatus.Validated,
                HasPublicSeoSignals = HasParkSeoSignals(park, projectCurrentParkForPublication),
            };
            contextsByParkId[parkId] = context;
        }

        return contextsByParkId;
    }

    public static async Task<IReadOnlyDictionary<string, ParkItemDataCompletenessContext>> BuildParkItemContextsAsync(
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyDictionary<string, Park> parentParksById,
        IParkZoneRepository? parkZoneRepository,
        IImageRepository? imageRepository,
        IHistoryEventRepository? historyEventRepository,
        CancellationToken cancellationToken)
    {
        List<string> itemIds = parkItems
            .Select(static item => item.Id)
            .Where(static itemId => !string.IsNullOrWhiteSpace(itemId))
            .Select(static itemId => itemId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (itemIds.Count == 0)
        {
            return new Dictionary<string, ParkItemDataCompletenessContext>(StringComparer.Ordinal);
        }

        List<string> parkIds = parkItems
            .Select(static item => item.ParkId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Dictionary<string, List<ParkZone>> zonesByParkId = await LoadZonesByParkIdAsync(parkIds, parkZoneRepository, cancellationToken);
        Dictionary<string, List<Image>> imagesByItemId = await LoadImagesByOwnerIdAsync(ImageOwnerType.ParkItem, itemIds, ImageCategory.ParkItem, imageRepository, cancellationToken);
        Dictionary<string, List<HistoryEvent>> historyByItemId = await LoadHistoryByOwnerIdAsync(HistoryEntityType.ParkItem, itemIds, historyEventRepository, cancellationToken);

        Dictionary<string, ParkItemDataCompletenessContext> contextsByItemId = new Dictionary<string, ParkItemDataCompletenessContext>(StringComparer.Ordinal);
        foreach (ParkItem parkItem in parkItems)
        {
            if (string.IsNullOrWhiteSpace(parkItem.Id))
            {
                continue;
            }

            string itemId = parkItem.Id.Trim();
            bool parentResolved = parentParksById.TryGetValue(parkItem.ParkId, out Park? parentPark);
            List<ParkZone> parentZones = zonesByParkId.GetValueOrDefault(parkItem.ParkId) ?? new List<ParkZone>();
            List<Image> itemImages = imagesByItemId.GetValueOrDefault(itemId) ?? new List<Image>();
            List<Image> publishedImages = itemImages.Where(static image => image.IsPublished).ToList();
            List<HistoryEvent> historyEvents = historyByItemId.GetValueOrDefault(itemId) ?? new List<HistoryEvent>();
            IReadOnlyCollection<HistoryEvent> visibleHistoryEvents = historyEvents
                .Where(static historyEvent => historyEvent.IsVisible)
                .ToList();

            ParkItemDataCompletenessContext context = new ParkItemDataCompletenessContext
            {
                ParentParkResolved = parentResolved,
                ParentParkVisible = parentPark?.IsVisible == true,
                HasOfficialZoneContext = parentZones.Count > 0,
                HasUsefulVisitGrouping = !string.IsNullOrWhiteSpace(parkItem.ZoneId) || parentZones.Count == 0,
                HasRepresentativeImage = publishedImages.Count > 0,
                HasResolvedImageOwner = publishedImages.Count > 0 && publishedImages.All(image => string.Equals(image.OwnerId, itemId, StringComparison.Ordinal)),
                HasLocalizedImageAltText = publishedImages.Any(static image => HasLocalizedText(image.AltTexts)),
                HasNonMisleadingImage = publishedImages.Count > 0,
                HasOriginalMedia = publishedImages.Any(static image => !string.IsNullOrWhiteSpace(image.OriginalFileName)),
                HasHistoricalImageContext = IsClosedParkItem(parkItem) && publishedImages.Count > 0,
                HistoryEventCount = historyEvents.Count,
                ClosureOrChangeHistoryEventCount = historyEvents.Count(IsClosureOrChangeEvent),
                PublishedArticleCount = historyEvents.Count(static historyEvent => HasPublishedArticle(historyEvent)),
                HistoryEventsWithSourcesCount = historyEvents.Count(HasSources),
                HasReferenceDetailsOrDocumentedDebt = !string.IsNullOrWhiteSpace(parkItem.AttractionDetails?.ManufacturerId)
                    || parkItem.AdminReviewStatus != AdminReviewStatus.Validated,
                HasInternalLinks = parentResolved,
                HasSeoSignals = HasParkItemSeoSignals(parkItem, parentResolved),
                HasStructuredDataSignals = parkItem.Category != ParkItemCategory.Other && parkItem.Type != ParkItemType.Other,
                HasHumanReviewOrDocumentedDebt = parkItem.AdminReviewStatus != AdminReviewStatus.ToReview,
                HasNoForbiddenPublicText = !publishedImages.Any(HasForbiddenImagePublicText)
                    && !visibleHistoryEvents.Any(static historyEvent => HasForbiddenHistoryPublicText(historyEvent, false)),
            };
            contextsByItemId[itemId] = context;
        }

        return contextsByItemId;
    }

    private static async Task<Dictionary<string, List<ParkZone>>> LoadZonesByParkIdAsync(
        IReadOnlyCollection<string> parkIds,
        IParkZoneRepository? parkZoneRepository,
        CancellationToken cancellationToken)
    {
        if (parkZoneRepository is null || parkIds.Count == 0)
        {
            return new Dictionary<string, List<ParkZone>>(StringComparer.Ordinal);
        }

        HashSet<string> parkIdSet = parkIds.ToHashSet(StringComparer.Ordinal);
        IReadOnlyCollection<ParkZone> zones = await parkZoneRepository.GetAllAsync(cancellationToken);
        return zones
            .Where(zone => !string.IsNullOrWhiteSpace(zone.ParkId) && parkIdSet.Contains(zone.ParkId.Trim()))
            .GroupBy(static zone => zone.ParkId.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
    }

    private static async Task<Dictionary<string, List<Image>>> LoadImagesByOwnerIdAsync(
        ImageOwnerType ownerType,
        IReadOnlyCollection<string> ownerIds,
        ImageCategory category,
        IImageRepository? imageRepository,
        CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (imageRepository is null || normalizedOwnerIds.Count == 0)
        {
            return new Dictionary<string, List<Image>>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<Image> images = await imageRepository.GetByOwnersAsync(ownerType, normalizedOwnerIds, category, cancellationToken);
        return images
            .Where(static image => !string.IsNullOrWhiteSpace(image.OwnerId))
            .GroupBy(static image => image.OwnerId!.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
    }

    private static async Task<Dictionary<string, List<HistoryEvent>>> LoadHistoryByOwnerIdAsync(
        HistoryEntityType entityType,
        IReadOnlyCollection<string> ownerIds,
        IHistoryEventRepository? historyEventRepository,
        CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (historyEventRepository is null || normalizedOwnerIds.Count == 0)
        {
            return new Dictionary<string, List<HistoryEvent>>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<HistoryEvent> events = await historyEventRepository.GetOwnerTimelinesAsync(entityType, normalizedOwnerIds, true, cancellationToken);
        return events
            .Where(static historyEvent => !string.IsNullOrWhiteSpace(historyEvent.OwnerId))
            .GroupBy(static historyEvent => historyEvent.OwnerId.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.Ordinal);
    }

    private static bool HasKnownStatusOrDates(ParkItem item)
    {
        return !string.IsNullOrWhiteSpace(item.AttractionDetails?.Status)
            || item.AttractionDetails?.OpeningDate is not null
            || item.AttractionDetails?.ClosingDate is not null
            || !string.IsNullOrWhiteSpace(item.AttractionDetails?.OpeningDateText)
            || !string.IsNullOrWhiteSpace(item.AttractionDetails?.ClosingDateText);
    }

    private static bool HasManufacturer(ParkItem item)
    {
        return item.Category == ParkItemCategory.Attraction
            && !string.IsNullOrWhiteSpace(item.AttractionDetails?.ManufacturerId);
    }

    private static bool HasAccessConditions(ParkItem item)
    {
        return item.AttractionDetails?.AccessConditions.Count > 0;
    }

    private static bool IsClosedImportantParkItem(ParkItem item)
    {
        return IsClosedParkItem(item)
            && item.Category == ParkItemCategory.Attraction;
    }

    private static bool IsClosedParkItem(ParkItem item)
    {
        string status = item.AttractionDetails?.Status ?? string.Empty;
        return status.Contains("closed", StringComparison.OrdinalIgnoreCase)
            || status.Contains("ferme", StringComparison.OrdinalIgnoreCase)
            || status.Contains("fermé", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCommercialOrService(ParkItem item)
    {
        return item.Category is ParkItemCategory.Restaurant
            or ParkItemCategory.Shop
            or ParkItemCategory.Service
            or ParkItemCategory.Hotel;
    }

    private static bool HasPublishedArticle(HistoryEvent historyEvent, bool projectForPublication = false)
    {
        return historyEvent.Article is not null
            && (projectForPublication || historyEvent.Article.IsPublished);
    }

    private static bool HasStructuredArticle(HistoryEvent historyEvent, bool projectForPublication = false)
    {
        return historyEvent.Article is not null
            && (projectForPublication || historyEvent.Article.IsPublished)
            && historyEvent.Article.Blocks.Count > 0;
    }

    private static bool HasLocalizedHistoryContent(HistoryEvent historyEvent)
    {
        return HasLocalizedText(historyEvent.Titles)
            || HasLocalizedText(historyEvent.Summaries)
            || HasLocalizedText(historyEvent.Article?.Titles)
            || HasLocalizedText(historyEvent.Article?.Summaries);
    }

    private static bool HasSources(HistoryEvent historyEvent)
    {
        return historyEvent.Sources.Any(static source => !string.IsNullOrWhiteSpace(source.Url))
            || historyEvent.Article?.Sources.Any(static source => !string.IsNullOrWhiteSpace(source.Url)) == true;
    }

    private static bool HasMedia(HistoryEvent historyEvent)
    {
        return !string.IsNullOrWhiteSpace(historyEvent.MainImageId)
            || !string.IsNullOrWhiteSpace(historyEvent.Article?.MainImageId)
            || historyEvent.Article?.Blocks.Any(static block => !string.IsNullOrWhiteSpace(block.ImageId) || block.ImageIds.Count > 0) == true;
    }

    private static bool HasNoForbiddenParkRelatedPublicText(
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> zones,
        IReadOnlyCollection<Image> parkImages,
        IReadOnlyCollection<Image> parkItemImages,
        IReadOnlyCollection<HistoryEvent> parkHistory,
        IReadOnlyCollection<HistoryEvent> parkItemHistory,
        bool projectForPublication)
    {
        return !parkItems
                .Where(static item => item.IsVisible)
                .Any(static item => HasForbiddenLocalizedPublicText(item.Descriptions))
            && !zones
                .Where(static zone => zone.IsVisible)
                .Any(static zone => HasForbiddenLocalizedPublicText(zone.Descriptions))
            && !parkImages.Any(HasForbiddenImagePublicText)
            && !parkItemImages.Any(HasForbiddenImagePublicText)
            && !parkHistory.Any(historyEvent => HasForbiddenHistoryPublicText(historyEvent, projectForPublication))
            && !parkItemHistory.Any(historyEvent => HasForbiddenHistoryPublicText(historyEvent, projectForPublication));
    }

    private static bool HasForbiddenImagePublicText(Image image)
    {
        return DataCompletenessScoringRules.HasForbiddenPublicText(image.Description)
            || HasForbiddenLocalizedPublicText(image.AltTexts)
            || HasForbiddenLocalizedPublicText(image.Captions);
    }

    private static bool HasForbiddenHistoryPublicText(HistoryEvent historyEvent, bool projectForPublication)
    {
        if (HasForbiddenLocalizedPublicText(historyEvent.Titles)
            || HasForbiddenLocalizedPublicText(historyEvent.Summaries))
        {
            return true;
        }

        HistoryArticle? article = historyEvent.Article;
        if (article is null || (!projectForPublication && !article.IsPublished))
        {
            return false;
        }

        return HasForbiddenLocalizedPublicText(article.Titles)
            || HasForbiddenLocalizedPublicText(article.Subtitles)
            || HasForbiddenLocalizedPublicText(article.Summaries)
            || article.Blocks.Any(static block =>
                HasForbiddenLocalizedPublicText(block.Texts)
                || HasForbiddenLocalizedPublicText(block.Captions));
    }

    private static bool HasForbiddenLocalizedPublicText(IEnumerable<LocalizedText> values)
    {
        return values.Any(static value => DataCompletenessScoringRules.HasForbiddenPublicText(value.Value));
    }

    private static int CountReferenceDescriptions(Park park, IReadOnlyCollection<ParkItem> parkItems)
    {
        int count = 0;
        if (!string.IsNullOrWhiteSpace(park.OperatorId))
        {
            count += 1;
        }

        if (!string.IsNullOrWhiteSpace(park.FounderId))
        {
            count += 1;
        }

        count += parkItems
            .Where(HasManufacturer)
            .Select(static item => item.AttractionDetails!.ManufacturerId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .Count();

        return count;
    }

    private static int CountReferenceDetails(Park park, IReadOnlyCollection<ParkItem> parkItems)
    {
        return CountReferenceDescriptions(park, parkItems);
    }

    private static bool HasCriticalSource(Park park, IReadOnlyCollection<HistoryEvent> historyEvents)
    {
        return !string.IsNullOrWhiteSpace(park.WebsiteUrl)
            || historyEvents.Any(HasSources);
    }

    private static bool HasParkSeoSignals(Park park, bool projectForPublication = false)
    {
        return park.IsVisible
            && (park.AdminReviewStatus == AdminReviewStatus.Validated
                || (projectForPublication && park.AdminReviewStatus != AdminReviewStatus.NotRelevant))
            && !string.IsNullOrWhiteSpace(park.Name)
            && HasLocalizedText(park.Descriptions);
    }

    private static bool HasParkItemSeoSignals(ParkItem parkItem, bool parentResolved)
    {
        return parkItem.IsVisible
            && parentResolved
            && parkItem.AdminReviewStatus == AdminReviewStatus.Validated
            && !string.IsNullOrWhiteSpace(parkItem.Name)
            && HasLocalizedText(parkItem.Descriptions);
    }

    private static bool IsClosureOrChangeEvent(HistoryEvent historyEvent)
    {
        return historyEvent.EventType.Contains("Closure", StringComparison.OrdinalIgnoreCase)
            || historyEvent.EventType.Contains("Change", StringComparison.OrdinalIgnoreCase)
            || historyEvent.EventType.Contains("Rename", StringComparison.OrdinalIgnoreCase)
            || historyEvent.EventType.Contains("Relocation", StringComparison.OrdinalIgnoreCase)
            || historyEvent.EventType.Contains("Refurbishment", StringComparison.OrdinalIgnoreCase)
            || historyEvent.EventType.Contains("Rehab", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasLocalizedText(IEnumerable<LocalizedText>? values)
    {
        return values?.Any(static value => !string.IsNullOrWhiteSpace(value.Value)) == true;
    }
}

internal sealed class ParkOpeningHoursAdminStatusResolverAccessor
{
    private readonly Func<ParkOpeningHoursScheduleSummary, ParkOpeningHoursAdminStatus> resolveStatus;

    public ParkOpeningHoursAdminStatusResolverAccessor(Func<ParkOpeningHoursScheduleSummary, ParkOpeningHoursAdminStatus> resolveStatus)
    {
        this.resolveStatus = resolveStatus;
    }

    public ParkOpeningHoursAdminStatus ResolveStatus(ParkOpeningHoursScheduleSummary summary)
    {
        return this.resolveStatus(summary);
    }
}
