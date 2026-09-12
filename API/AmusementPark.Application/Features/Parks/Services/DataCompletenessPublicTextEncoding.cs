using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.Parks.Services;

internal static class DataCompletenessPublicTextEncoding
{
    internal static async Task<Dictionary<string, ParkFounder>> LoadFoundersByIdAsync(
        IReadOnlyCollection<Park> parks,
        IParkFounderRepository? repository,
        CancellationToken cancellationToken)
    {
        List<string> ids = parks
            .Select(static park => park.FounderId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (repository is null || ids.Count == 0)
        {
            return new Dictionary<string, ParkFounder>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<ParkFounder> founders = await repository.GetByIdsAsync(ids, cancellationToken);
        return founders
            .Where(static founder => !string.IsNullOrWhiteSpace(founder.Id))
            .GroupBy(static founder => founder.Id!.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
    }

    internal static async Task<Dictionary<string, ParkOperator>> LoadOperatorsByIdAsync(
        IReadOnlyCollection<Park> parks,
        IParkOperatorRepository? repository,
        CancellationToken cancellationToken)
    {
        List<string> ids = parks
            .Select(static park => park.OperatorId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (repository is null || ids.Count == 0)
        {
            return new Dictionary<string, ParkOperator>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<ParkOperator> operators = await repository.GetByIdsAsync(ids, cancellationToken);
        return operators
            .Where(static parkOperator => !string.IsNullOrWhiteSpace(parkOperator.Id))
            .GroupBy(static parkOperator => parkOperator.Id!.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
    }

    internal static async Task<Dictionary<string, AttractionManufacturer>> LoadManufacturersByIdAsync(
        IReadOnlyCollection<ParkItem> parkItems,
        IAttractionManufacturerRepository? repository,
        CancellationToken cancellationToken)
    {
        List<string> ids = parkItems
            .Where(static item => item.IsVisible && item.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .Select(static item => item.AttractionDetails?.ManufacturerId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (repository is null || ids.Count == 0)
        {
            return new Dictionary<string, AttractionManufacturer>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<AttractionManufacturer> manufacturers = await repository.GetByIdsAsync(ids, cancellationToken);
        return manufacturers
            .Where(static manufacturer => manufacturer.IsVisible && !string.IsNullOrWhiteSpace(manufacturer.Id))
            .GroupBy(static manufacturer => manufacturer.Id!.Trim(), StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);
    }

    internal static T? ResolveReference<T>(string? id, IReadOnlyDictionary<string, T> referencesById)
        where T : class
    {
        return !string.IsNullOrWhiteSpace(id)
            ? referencesById.GetValueOrDefault(id.Trim())
            : null;
    }

    internal static IReadOnlyCollection<AttractionManufacturer> ResolveManufacturers(
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyDictionary<string, AttractionManufacturer> manufacturersById)
    {
        return parkItems
            .Where(static item => item.IsVisible && item.AdminReviewStatus != AdminReviewStatus.NotRelevant)
            .Select(static item => item.AttractionDetails?.ManufacturerId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(id => manufacturersById.GetValueOrDefault(id!.Trim()))
            .Where(static manufacturer => manufacturer is not null)
            .Cast<AttractionManufacturer>()
            .DistinctBy(static manufacturer => manufacturer.Id, StringComparer.Ordinal)
            .ToList();
    }

    internal static IReadOnlyCollection<Image> ResolveReferenceImages(
        ParkFounder? founder,
        ParkOperator? parkOperator,
        IReadOnlyCollection<AttractionManufacturer> manufacturers,
        IReadOnlyDictionary<string, List<Image>> founderImagesById,
        IReadOnlyDictionary<string, List<Image>> operatorImagesById,
        IReadOnlyDictionary<string, List<Image>> manufacturerImagesById)
    {
        List<Image> images = new List<Image>();
        AddPublishedReferenceImages(images, founder?.Id, founderImagesById);
        AddPublishedReferenceImages(images, parkOperator?.Id, operatorImagesById);
        foreach (AttractionManufacturer manufacturer in manufacturers)
        {
            AddPublishedReferenceImages(images, manufacturer.Id, manufacturerImagesById);
        }

        return images;
    }

    private static void AddPublishedReferenceImages(
        ICollection<Image> images,
        string? ownerId,
        IReadOnlyDictionary<string, List<Image>> imagesByOwnerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        foreach (Image image in imagesByOwnerId.GetValueOrDefault(ownerId.Trim()) ?? new List<Image>())
        {
            if (image.IsPublished)
            {
                images.Add(image);
            }
        }
    }

    internal static bool HasNoForbiddenParkRelatedPublicText(
        IReadOnlyCollection<ParkItem> parkItems,
        IReadOnlyCollection<ParkZone> zones,
        IReadOnlyCollection<Image> parkImages,
        IReadOnlyCollection<Image> parkItemImages,
        IReadOnlyCollection<HistoryEvent> parkHistory,
        IReadOnlyCollection<HistoryEvent> parkItemHistory,
        bool projectForPublication,
        ParkOpeningHoursSchedule? openingHoursSchedule,
        ParkPricingEntity? pricing,
        ParkFounder? founder,
        ParkOperator? parkOperator,
        IReadOnlyCollection<AttractionManufacturer> manufacturers,
        IReadOnlyCollection<Image> referenceImages)
    {
        return !parkItems
                .Where(static item => item.IsVisible && item.AdminReviewStatus != AdminReviewStatus.NotRelevant)
                .Any(static item =>
                    DataCompletenessScoringRules.HasForbiddenPlainPublicText(item.Name)
                    || DataCompletenessScoringRules.HasForbiddenPlainPublicText(item.Subtype)
                    || HasForbiddenLocalizedRichPublicText(item.Descriptions)
                    || HasForbiddenAttractionDetailPublicText(item)
                    || HasForbiddenAccessConditionPublicText(item))
            && !zones
                .Where(static zone => zone.IsVisible)
                .Any(static zone =>
                    DataCompletenessScoringRules.HasForbiddenPlainPublicText(zone.Name)
                    || HasForbiddenLocalizedPlainPublicText(zone.Names)
                    || HasForbiddenLocalizedRichPublicText(zone.Descriptions))
            && !parkImages.Any(HasForbiddenImagePublicText)
            && !parkItemImages.Any(HasForbiddenImagePublicText)
            && !parkHistory.Any(historyEvent => HasForbiddenHistoryPublicText(historyEvent, projectForPublication))
            && !parkItemHistory.Any(historyEvent => HasForbiddenHistoryPublicText(historyEvent, projectForPublication))
            && !HasForbiddenOpeningHoursPublicText(openingHoursSchedule)
            && !HasForbiddenPricingPublicText(pricing)
            && !HasForbiddenFounderPublicText(founder)
            && !HasForbiddenOperatorPublicText(parkOperator)
            && !manufacturers.Any(HasForbiddenManufacturerPublicText)
            && !referenceImages.Any(HasForbiddenImagePublicText);
    }

    private static bool HasForbiddenOpeningHoursPublicText(ParkOpeningHoursSchedule? schedule)
    {
        return schedule is not null
            && (schedule.RegularRules.Any(static rule =>
                    HasForbiddenLocalizedPlainPublicText(rule.Labels)
                    || HasForbiddenLocalizedPlainPublicText(rule.Reasons))
                || schedule.DateOverrides.Any(static dateOverride =>
                    HasForbiddenLocalizedPlainPublicText(dateOverride.Labels)
                    || HasForbiddenLocalizedPlainPublicText(dateOverride.Reasons)));
    }

    private static bool HasForbiddenPricingPublicText(ParkPricingEntity? pricing)
    {
        if (pricing is null)
        {
            return false;
        }

        DateOnly currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        ParkPricingEntity publicPricing = pricing.FilterOffersValidOn(currentDate);
        return HasForbiddenLocalizedPlainPublicText(publicPricing.Notes)
                || HasForbiddenPricingOffersPublicText(
                    publicPricing.AdmissionOffers,
                    publicPricing.AnnualPasses,
                    publicPricing.ParkingOffers,
                    publicPricing.CreditOffers)
                || publicPricing.HistoricalSnapshots
                    .OrderByDescending(static snapshot => snapshot.Year)
                    .Take(10)
                    .Any(static snapshot =>
                    HasForbiddenLocalizedPlainPublicText(snapshot.Notes)
                    || HasForbiddenPricingOffersPublicText(
                        snapshot.AdmissionOffers,
                        snapshot.AnnualPasses,
                        snapshot.ParkingOffers,
                        snapshot.CreditOffers));
    }

    private static bool HasForbiddenFounderPublicText(ParkFounder? founder)
    {
        return founder is not null
            && (DataCompletenessScoringRules.HasForbiddenPlainPublicText(founder.Name)
                || DataCompletenessScoringRules.HasForbiddenPlainPublicText(founder.Occupation)
                || DataCompletenessScoringRules.HasForbiddenPlainPublicText(founder.BirthDate)
                || DataCompletenessScoringRules.HasForbiddenPlainPublicText(founder.DeathDate)
                || DataCompletenessScoringRules.HasForbiddenPlainPublicText(founder.BirthPlace)
                || HasForbiddenLocalizedRichPublicText(founder.Biography));
    }

    private static bool HasForbiddenOperatorPublicText(ParkOperator? parkOperator)
    {
        return parkOperator is not null
            && (DataCompletenessScoringRules.HasForbiddenPlainPublicText(parkOperator.Name)
                || DataCompletenessScoringRules.HasForbiddenPlainPublicText(parkOperator.LegalName)
                || HasForbiddenContactDetailsPublicText(parkOperator.ContactDetails)
                || HasForbiddenLocalizedRichPublicText(parkOperator.Description));
    }

    private static bool HasForbiddenManufacturerPublicText(AttractionManufacturer manufacturer)
    {
        return DataCompletenessScoringRules.HasForbiddenPlainPublicText(manufacturer.Name)
            || DataCompletenessScoringRules.HasForbiddenPlainPublicText(manufacturer.LegalName)
            || HasForbiddenContactDetailsPublicText(manufacturer.ContactDetails)
            || HasForbiddenLocalizedRichPublicText(manufacturer.Biography);
    }

    private static bool HasForbiddenContactDetailsPublicText(ParkReferenceContactDetails? contactDetails)
    {
        return contactDetails is not null
            && (DataCompletenessScoringRules.HasHtmlEntity(contactDetails.Email)
                || DataCompletenessScoringRules.HasHtmlEntity(contactDetails.PhoneNumber)
                || DataCompletenessScoringRules.HasHtmlEntity(contactDetails.Street)
                || DataCompletenessScoringRules.HasHtmlEntity(contactDetails.City)
                || DataCompletenessScoringRules.HasHtmlEntity(contactDetails.PostalCode));
    }

    private static bool HasForbiddenPricingOffersPublicText(
        IEnumerable<ParkAdmissionPriceOffer> admissionOffers,
        IEnumerable<ParkAnnualPassOffer> annualPasses,
        IEnumerable<ParkParkingPriceOffer> parkingOffers,
        IEnumerable<ParkCreditOffer> creditOffers)
    {
        return admissionOffers.Any(static offer =>
                HasForbiddenLocalizedPlainPublicText(offer.Labels)
                || HasForbiddenLocalizedPlainPublicText(offer.Conditions))
            || annualPasses.Any(static offer =>
                HasForbiddenLocalizedPlainPublicText(offer.Names)
                || HasForbiddenLocalizedPlainPublicText(offer.Conditions))
            || parkingOffers.Any(static offer =>
                HasForbiddenLocalizedPlainPublicText(offer.Labels)
                || HasForbiddenLocalizedPlainPublicText(offer.Conditions))
            || creditOffers.Any(static offer =>
                HasForbiddenLocalizedPlainPublicText(offer.Labels)
                || HasForbiddenLocalizedPlainPublicText(offer.Conditions));
    }

    private static bool HasForbiddenAccessConditionPublicText(ParkItem item)
    {
        return item.AttractionDetails?.AccessConditions.Any(static condition =>
            HasForbiddenLocalizedPlainPublicText(condition.CustomTypeLabel)
            || HasForbiddenLocalizedPlainPublicText(condition.Label)
            || HasForbiddenLocalizedPlainPublicText(condition.Description)) == true;
    }

    private static bool HasForbiddenAttractionDetailPublicText(ParkItem item)
    {
        AttractionDetails? details = item.AttractionDetails;
        return details is not null
            && (DataCompletenessScoringRules.HasHtmlEntity(details.Model)
                || DataCompletenessScoringRules.HasHtmlEntity(details.Status)
                || DataCompletenessScoringRules.HasHtmlEntity(details.MaterialType)
                || DataCompletenessScoringRules.HasHtmlEntity(details.SeatingType)
                || DataCompletenessScoringRules.HasHtmlEntity(details.LaunchType)
                || DataCompletenessScoringRules.HasHtmlEntity(details.RestraintType)
                || DataCompletenessScoringRules.HasHtmlEntity(details.OpeningDateText)
                || DataCompletenessScoringRules.HasHtmlEntity(details.ClosingDateText));
    }

    internal static bool HasForbiddenImagePublicText(Image image)
    {
        return DataCompletenessScoringRules.HasForbiddenPlainPublicText(image.Description)
            || HasForbiddenLocalizedPlainPublicText(image.AltTexts)
            || HasForbiddenLocalizedPlainPublicText(image.Captions)
            || HasForbiddenLocalizedPlainPublicText(image.Credits);
    }

    internal static bool HasForbiddenHistoryPublicText(HistoryEvent historyEvent, bool projectForPublication)
    {
        if (HasForbiddenLocalizedPlainPublicText(historyEvent.Titles)
            || HasForbiddenLocalizedPlainPublicText(historyEvent.Summaries)
            || historyEvent.Sources.Any(static source => DataCompletenessScoringRules.HasForbiddenPlainPublicText(source.Label)))
        {
            return true;
        }

        HistoryArticle? article = historyEvent.Article;
        if (article is null || (!projectForPublication && !article.IsPublished))
        {
            return false;
        }

        return HasForbiddenLocalizedPlainPublicText(article.Titles)
            || HasForbiddenLocalizedPlainPublicText(article.Subtitles)
            || HasForbiddenLocalizedPlainPublicText(article.Summaries)
            || article.Sources.Any(static source => DataCompletenessScoringRules.HasForbiddenPlainPublicText(source.Label))
            || article.Blocks.Any(static block =>
                HasForbiddenLocalizedPlainPublicText(block.Texts)
                || HasForbiddenLocalizedPlainPublicText(block.Captions));
    }

    private static bool HasForbiddenLocalizedPlainPublicText(IEnumerable<LocalizedText> values)
    {
        return values.Any(static value => DataCompletenessScoringRules.HasForbiddenPlainPublicText(value.Value));
    }

    private static bool HasForbiddenLocalizedRichPublicText(IEnumerable<LocalizedText> values)
    {
        return values.Any(static value => DataCompletenessScoringRules.HasForbiddenRichPublicText(value.Value));
    }

}
