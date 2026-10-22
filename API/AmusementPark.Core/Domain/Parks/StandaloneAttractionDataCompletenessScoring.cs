using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

internal static class StandaloneAttractionDataCompletenessScoring
{
    internal static DataCompletenessScore Calculate(
        StandaloneAttraction attraction,
        StandaloneAttractionDataCompletenessContext context)
    {
        bool projected = context.ProjectForPublication;
        bool operating = ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status);
        DateOnly today = DateOnly.FromDateTime(context.AsOfUtc);
        AttractionDetails? details = attraction.AttractionDetails;
        Image[] images = context.Images.Where(image =>
            image.OwnerType == ImageOwnerType.StandaloneAttraction
            && Matches(attraction.Id, image.OwnerId)
            && (projected || image.IsPublished)).ToArray();
        HistoryEvent[] history = context.HistoryEvents.Where(historyEvent =>
            historyEvent.EntityType == HistoryEntityType.StandaloneAttraction
            && Matches(attraction.Id, historyEvent.OwnerId)
            && (projected || historyEvent.IsVisible)).ToArray();
        HistoryArticle[] articles = history.Select(static entry => entry.Article)
            .OfType<HistoryArticle>().Where(article => projected || article.IsPublished).ToArray();
        ParkOperator? parkOperator = Matches(attraction.OperatorId, context.Operator?.Id)
            && context.Operator?.AdminReviewStatus != AdminReviewStatus.NotRelevant ? context.Operator : null;
        AttractionManufacturer? manufacturer = Matches(details?.ManufacturerId, context.Manufacturer?.Id)
            && context.Manufacturer?.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && (projected || context.Manufacturer?.IsVisible == true) ? context.Manufacturer : null;
        Image[] referenceImages = context.ReferenceImages.Where(image =>
            (projected || image.IsPublished)
            && ((image.OwnerType == ImageOwnerType.ParkOperator && Matches(parkOperator?.Id, image.OwnerId))
                || (image.OwnerType == ImageOwnerType.AttractionManufacturer && Matches(manufacturer?.Id, image.OwnerId))))
            .ToArray();
        ParkPricing? pricing = operating && Matches(attraction.Id, context.Pricing?.ParkId)
            ? context.Pricing!.FilterOffersValidOn(today) : null;
        ParkOpeningHoursSchedule? hours = operating && Matches(attraction.Id, context.OpeningHours?.ParkId)
            ? context.OpeningHours : null;
        DataCompletenessScoreBuilder score = new();
        score.AddPublicationBlocker(attraction.AdminReviewStatus == AdminReviewStatus.NotRelevant,
            "standalone-attraction.not-relevant");
        score.AddPublicationBlocker(
            (DataCompletenessScoringRules.HasText(attraction.OperatorId) && parkOperator is null)
            || (DataCompletenessScoringRules.HasText(details?.ManufacturerId) && manufacturer is null),
            "references.unresolved");

        score.Add(DataCompletenessScoringRules.HasText(attraction.Name)
            && !DataCompletenessScoringRules.IsPlaceholderName(attraction.Name), 4);
        score.Add(DataCompletenessScoringRules.HasText(attraction.CountryCode), 2);
        score.Add(DataCompletenessScoringRules.HasText(attraction.City), 2);
        score.Add(DataCompletenessScoringRules.HasValidPosition(attraction.Position), 4);
        score.Add(attraction.AdminReviewStatus != AdminReviewStatus.NotRelevant, 2);
        score.Add(projected || attraction.IsVisible, 2);
        score.Add(Enum.IsDefined(attraction.Type) && attraction.Type != ParkItemType.Other
            && (attraction.Type != ParkItemType.Attraction || DataCompletenessScoringRules.HasText(attraction.Subtype)), 2);
        string? status = ParkItemStatusNormalizer.Normalize(details?.Status);
        score.Add(status is ParkItemStatusNormalizer.Operating or ParkItemStatusNormalizer.Planned
            or ParkItemStatusNormalizer.UnderConstruction or ParkItemStatusNormalizer.TemporarilyClosed
            or ParkItemStatusNormalizer.ClosedDefinitively or ParkItemStatusNormalizer.Removed, 2);
        score.Add(details?.OpeningDate is not null || DataCompletenessScoringRules.HasText(details?.OpeningDateText), 2);
        score.AddIfApplicable(status is ParkItemStatusNormalizer.ClosedDefinitively or ParkItemStatusNormalizer.Removed,
            details?.ClosingDate is not null || DataCompletenessScoringRules.HasText(details?.ClosingDateText), 2);
        score.Add(HasSource(details?.SourceUrl), 2);
        score.AddIfApplicable(operating, HasSource(attraction.WebsiteUrl), 2);
        score.AddPartial(DataCompletenessScoringRules.CountPublicLanguagesWithText(attraction.Descriptions), 8, 8);
        score.Add(AllLanguages(attraction.Descriptions.Where(static text =>
            DataCompletenessScoringRules.HasMeaningfulText(text.Value))), 4);

        bool mechanical = attraction.Type is ParkItemType.RollerCoaster or ParkItemType.WaterRide
            or ParkItemType.FlatRide or ParkItemType.DarkRide or ParkItemType.FamilyRide
            or ParkItemType.ThrillRide or ParkItemType.TransportRide or ParkItemType.ObservationRide or ParkItemType.DropTower;
        score.AddIfApplicable(mechanical, manufacturer is not null, 2);
        score.AddIfApplicable(mechanical, DataCompletenessScoringRules.HasText(details?.Model), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasText(attraction.OperatorId), parkOperator is not null, 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasText(attraction.OperatorId),
            parkOperator is not null && AllLanguages(parkOperator.Description), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasText(details?.ManufacturerId), manufacturer is not null, 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasText(details?.ManufacturerId),
            manufacturer is not null && AllLanguages(manufacturer.Biography), 2);

        AttractionAccessCondition[] conditions = details?.AccessConditions.Where(condition =>
            (!condition.EffectiveFrom.HasValue || condition.EffectiveFrom <= today)
            && (!condition.EffectiveTo.HasValue || condition.EffectiveTo >= today)).ToArray() ?? [];
        score.AddIfApplicable(operating, conditions.Length > 0, 2);
        score.AddIfApplicable(conditions.Length > 0,
            conditions.All(static condition => AllLanguages(condition.Label)
                && (!DataCompletenessScoringRules.HasAnyLocalizedText(condition.Description) || AllLanguages(condition.Description))), 2);
        score.AddIfApplicable(conditions.Length > 0, conditions.All(condition =>
            HasSource(condition.SourceUrl)
            && condition.SourceKind is AttractionAccessConditionSourceKind.Official
                or AttractionAccessConditionSourceKind.OperatorProvided or AttractionAccessConditionSourceKind.VerifiedSecondary
            && condition.SourceConfidence is AttractionAccessConditionConfidence.Medium or AttractionAccessConditionConfidence.High
            && condition.VerifiedAtUtc.HasValue && condition.VerifiedAtUtc <= context.AsOfUtc
            && AllLanguages(condition.SourceSummary)), 2);

        score.Add(images.Any(static image => image.IsCurrent && DataCompletenessScoringRules.HasText(image.Id)), 4);
        score.AddIfApplicable(images.Length > 0, images.All(static image =>
            AllLanguages(image.AltTexts) && AllLanguages(image.Captions)), 2);
        score.AddIfApplicable(images.Length > 0, images.All(static image =>
            HasSource(image.SourceUrl) && DataCompletenessScoringRules.HasAnyLocalizedText(image.Credits)), 2);
        score.Add(history.Length > 0, 2);
        score.AddIfApplicable(history.Length > 0, history.All(static entry =>
            entry.Year > 0 && AllLanguages(entry.Titles) && AllLanguages(entry.Summaries)
            && entry.Sources.Any(static source => HasSource(source.Url))), 4);
        score.AddIfApplicable(articles.Length > 0, articles.All(static article =>
            AllLanguages(article.Titles) && AllLanguages(article.Summaries)
            && article.Sources.Any(static source => HasSource(source.Url))
            && article.Blocks.Count(static block => block.Type == HistoryArticleBlockType.Paragraph && AllLanguages(block.Texts)) >= 3
            && article.Blocks.Count(static block => block.Type == HistoryArticleBlockType.Heading && AllLanguages(block.Texts)) >= 2), 4);
        HashSet<string> imageIds = images.Concat(referenceImages).Where(static image => DataCompletenessScoringRules.HasText(image.Id))
            .Select(static image => image.Id!).ToHashSet(StringComparer.Ordinal);
        bool resolvedHistoryImages = history.All(entry =>
            ResolvesOptionalImage(entry.MainImageId, imageIds))
            && articles.All(article => ResolvesOptionalImage(article.MainImageId, imageIds)
                && article.Blocks.All(block => ResolvesOptionalImage(block.ImageId, imageIds)
                    && block.ImageIds.All(imageIds.Contains)));
        score.AddIfApplicable(history.Length > 0, resolvedHistoryImages, 2);
        score.AddPublicationBlocker(!resolvedHistoryImages, "media.unresolved-reference");

        if (operating)
        {
            score.Add(pricing is not null && HasAdmissionPrice(pricing), 4);
            score.Add(pricing is not null && HasSource(pricing.SourceUrl)
                && IsRecentlyVerified(pricing.LastVerifiedAtUtc, context.AsOfUtc)
                && DataCompletenessScoringRules.HasText(pricing.CurrencyCode), 2);
            score.Add(pricing is not null && HasAdmissionPrice(pricing)
                && pricing.AdmissionOffers.All(static offer => AllLanguages(offer.Labels))
                && pricing.CreditOffers.All(static offer => AllLanguages(offer.Labels)), 2);
            score.Add(hours is not null && HasUsableSchedule(hours, today, context.AsOfUtc), 4);
            score.Add(hours is not null && HasSource(hours.SourceUrl)
                && DataCompletenessScoringRules.HasText(hours.TimeZoneId)
                && IsRecentlyVerified(hours.LastVerifiedAtUtc, context.AsOfUtc), 2);
        }

        bool forbidden = StandaloneAttractionPublicTextQuality.HasForbiddenText(
            attraction, images.Concat(referenceImages), history, articles, parkOperator, manufacturer, pricing, hours);
        FormulaicPublicTextIssue? formulaic = DataCompletenessScoringRules.FindFormulaicPublicTextIssue(
            StandaloneAttractionPublicTextQuality.GetNarrativeTexts(attraction, images.Concat(referenceImages), history, articles, parkOperator, manufacturer),
            new[] { attraction.Name, parkOperator?.Name, parkOperator?.LegalName, manufacturer?.Name, manufacturer?.LegalName });
        score.AddPublicationBlocker(forbidden, DataCompletenessScoringRules.ForbiddenPublicTextPublicationBlocker);
        score.AddPublicationBlocker(formulaic is not null, DataCompletenessScoringRules.FormulaicPublicTextPublicationBlocker);
        return score.Build() with { FormulaicPublicTextIssue = formulaic };
    }

    private static bool Matches(string? expected, string? actual) =>
        !string.IsNullOrWhiteSpace(expected) && string.Equals(expected, actual, StringComparison.Ordinal);

    private static bool AllLanguages(IEnumerable<LocalizedText> texts) =>
        DataCompletenessScoringRules.CountPublicLanguagesWithText(texts) == DataCompletenessScoringRules.PublicLanguageCount;

    private static bool HasSource(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https";

    private static bool IsRecentlyVerified(DateTime? verified, DateTime asOf) =>
        verified.HasValue && verified <= asOf && verified >= asOf.AddDays(-90);

    private static bool ResolvesOptionalImage(string? id, HashSet<string> images) =>
        string.IsNullOrWhiteSpace(id) || images.Contains(id);

    private static bool HasAdmissionPrice(ParkPricing pricing) =>
        pricing.AdmissionOffers.Any(static offer => HasValidPrice(offer.OnlinePrice) || HasValidPrice(offer.GatePrice))
        || pricing.CreditOffers.Any(static offer => offer.Quantity > 0
            && (offer.Prices.OnlinePrice is >= 0 || offer.Prices.GatePrice is >= 0));

    private static bool HasValidPrice(ParkPriceValue? price) =>
        price is not null && ParkPriceValueRules.Normalize(price).Error is null;

    private static bool HasUsableSchedule(ParkOpeningHoursSchedule hours, DateOnly today, DateTime asOf) =>
        hours.RegularRules.Any(rule => rule.EndDate >= today && rule.StartDate <= rule.EndDate
            && HasFutureWeekday(rule, today) && (rule.IsClosed || rule.TimeRanges.Count > 0))
        || hours.DateOverrides.Any(day => day.LocalDate >= today && (day.IsClosed || day.TimeRanges.Count > 0))
        || (HasSource(hours.SourceUrl) && IsRecentlyVerified(hours.LastVerifiedAtUtc, asOf)
            && hours.WeeklyRules.Any(static rule => rule.DaysOfWeek.Count > 0
                && rule.DaysOfWeek.All(static day => Enum.IsDefined(day))
                && (rule.IsClosed || rule.TimeRanges.Count > 0)));

    private static bool HasFutureWeekday(ParkOpeningHoursRule rule, DateOnly today)
    {
        DateOnly start = rule.StartDate > today ? rule.StartDate : today;
        int dayCount = Math.Min(7, rule.EndDate.DayNumber - start.DayNumber + 1);
        return Enumerable.Range(0, dayCount).Any(offset => rule.DaysOfWeek.Contains(start.AddDays(offset).DayOfWeek));
    }
}
