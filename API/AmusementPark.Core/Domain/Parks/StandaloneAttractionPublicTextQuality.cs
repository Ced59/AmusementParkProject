using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

internal static class StandaloneAttractionPublicTextQuality
{
    internal static bool HasForbiddenText(
        StandaloneAttraction attraction,
        IEnumerable<Image> images,
        IEnumerable<HistoryEvent> history,
        IEnumerable<HistoryArticle> articles,
        ParkOperator? parkOperator,
        AttractionManufacturer? manufacturer,
        ParkPricing? pricing,
        ParkOpeningHoursSchedule? hours)
    {
        AttractionDetails? details = attraction.AttractionDetails;
        return Structured(new[] { attraction.Name, attraction.Subtype, attraction.Street, attraction.City, attraction.PostalCode })
            || Rich(attraction.Descriptions)
            || Structured(new[] { details?.Model, details?.Status, details?.MaterialType, details?.SeatingType,
                details?.LaunchType, details?.RestraintType, details?.OpeningDateText, details?.ClosingDateText })
            || details?.AccessConditions.Any(static condition => Structured(condition.CustomTypeLabel
                .Concat(condition.Label).Concat(condition.Description).Concat(condition.SourceSummary))
                || Structured(new[] { condition.ScopeDetail })) == true
            || images.Any(static image => Plain(new[] { image.Description, image.OriginalFileName })
                || Plain(image.AltTexts.Concat(image.Captions).Concat(image.Credits)))
            || history.Any(static entry => Plain(entry.Titles.Concat(entry.Summaries))
                || Plain(new[] { entry.PreviousName, entry.NewName, entry.LocationLabel })
                || Plain(entry.Sources.Select(static source => source.Label)))
            || articles.Any(static article => Plain(article.Titles.Concat(article.Subtitles).Concat(article.Summaries))
                || Plain(article.Sources.Select(static source => source.Label))
                || article.Blocks.Any(static block => Plain(block.Texts.Concat(block.Captions))))
            || (parkOperator is not null && (Plain(new[] { parkOperator.Name, parkOperator.LegalName })
                || Rich(parkOperator.Description) || ForbiddenContact(parkOperator.ContactDetails)))
            || (manufacturer is not null && (Plain(new[] { manufacturer.Name, manufacturer.LegalName })
                || Rich(manufacturer.Biography) || ForbiddenContact(manufacturer.ContactDetails)))
            || (pricing is not null && (Structured(pricing.Notes)
                || ForbiddenOffers(pricing.AdmissionOffers, pricing.AnnualPasses, pricing.ParkingOffers, pricing.CreditOffers)
                || pricing.HistoricalSnapshots.OrderByDescending(static snapshot => snapshot.Year).Take(10).Any(static snapshot =>
                    Structured(snapshot.Notes)
                    || ForbiddenOffers(snapshot.AdmissionOffers, snapshot.AnnualPasses, snapshot.ParkingOffers, snapshot.CreditOffers))))
            || (hours is not null && (Structured(new[] { hours.Notes })
                || hours.RegularRules.Any(static rule => Structured(rule.Labels.Concat(rule.Reasons)))
                || hours.WeeklyRules.Any(static rule => Structured(rule.Labels.Concat(rule.Reasons)))
                || hours.DateOverrides.Any(static day => Structured(day.Labels.Concat(day.Reasons)))));
    }

    // Structured access/price conditions legitimately repeat and are checked separately above.
    internal static IEnumerable<LocalizedText> GetNarrativeTexts(
        StandaloneAttraction attraction,
        IEnumerable<Image> images,
        IEnumerable<HistoryEvent> history,
        IEnumerable<HistoryArticle> articles,
        ParkOperator? parkOperator,
        AttractionManufacturer? manufacturer)
    {
        return attraction.Descriptions
            .Concat(parkOperator?.Description ?? Enumerable.Empty<LocalizedText>())
            .Concat(manufacturer?.Biography ?? Enumerable.Empty<LocalizedText>())
            .Concat(images.SelectMany(static image => image.AltTexts.Concat(image.Captions)
                .Append(new LocalizedText("und", image.Description))))
            .Concat(history.SelectMany(static entry => entry.Summaries))
            .Concat(articles.SelectMany(static article => article.Subtitles.Concat(article.Summaries)
                .Concat(article.Blocks.Where(static block => block.Type is HistoryArticleBlockType.Paragraph or HistoryArticleBlockType.Quote)
                    .SelectMany(static block => block.Texts))
                .Concat(article.Blocks.SelectMany(static block => block.Captions))));
    }

    private static bool ForbiddenContact(ParkReferenceContactDetails? contact) => contact is not null
        && Structured(new[] { contact.Email, contact.PhoneNumber, contact.Street, contact.City, contact.PostalCode });

    private static bool ForbiddenOffers(
        IEnumerable<ParkAdmissionPriceOffer> admission,
        IEnumerable<ParkAnnualPassOffer> passes,
        IEnumerable<ParkParkingPriceOffer> parking,
        IEnumerable<ParkCreditOffer> credits) =>
        admission.Any(static offer => Structured(offer.Labels.Concat(offer.Conditions)))
        || passes.Any(static offer => Structured(offer.Names.Concat(offer.Conditions)))
        || parking.Any(static offer => Structured(offer.Labels.Concat(offer.Conditions)))
        || credits.Any(static offer => Structured(offer.Labels.Concat(offer.Conditions)));

    private static bool Rich(IEnumerable<LocalizedText> texts) =>
        texts.Any(static text => DataCompletenessScoringRules.HasForbiddenRichPublicText(text.Value));

    private static bool Plain(IEnumerable<LocalizedText> texts) => Plain(texts.Select(static text => text.Value));

    private static bool Plain(IEnumerable<string?> texts) => texts.Any(DataCompletenessScoringRules.HasForbiddenPlainPublicText);

    private static bool Structured(IEnumerable<LocalizedText> texts) => Structured(texts.Select(static text => text.Value));

    private static bool Structured(IEnumerable<string?> texts) => texts.Any(DataCompletenessScoringRules.HasForbiddenStructuredLabelPublicText);
}
