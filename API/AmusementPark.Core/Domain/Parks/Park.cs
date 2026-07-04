using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Agrégat métier représentant un parc.
/// </summary>
public sealed class Park : GeolocatedEntityBase
{
    /// <summary>
    /// Nom principal du parc.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Code pays ISO alpha-2.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Type de parc.
    /// </summary>
    public ParkType? Type { get; set; }

    /// <summary>
    /// Classification du rayonnement visiteur du parc.
    /// </summary>
    public ParkAudienceClassification? AudienceClassification { get; set; }

    public ParkStatus Status { get; set; } = ParkStatus.Operating;

    public DateTime? OpeningDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    public string? OpeningDateText { get; set; }

    public string? ClosingDateText { get; set; }

    /// <summary>
    /// Identifiant du fondateur associé.
    /// </summary>
    public string? FounderId { get; set; }

    /// <summary>
    /// Identifiant de l'exploitant associé.
    /// </summary>
    public string? OperatorId { get; set; }

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Indique si le parc est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Statut de traitement interne pour les listes d'administration.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    /// <summary>
    /// Indique si le parc est mis en avant manuellement sur la home publique.
    /// </summary>
    public bool IsFeaturedOnHome { get; set; }

    /// <summary>
    /// Ordre d'affichage manuel sur la home publique.
    /// </summary>
    public int? FeaturedHomeOrder { get; set; }

    /// <summary>
    /// Indique si la mise en avant home doit être présentée comme sponsorisée.
    /// </summary>
    public bool IsFeaturedOnHomeSponsored { get; set; }

    /// <summary>
    /// URL du site officiel.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Rue de l'adresse postale.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// Ville de l'adresse postale.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Code postal de l'adresse postale.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Identifiant de l'image de logo courante.
    /// </summary>
    public string? CurrentLogoImageId { get; set; }

    public DataCompletenessScore CalculateDataCompletenessScore(ParkDataCompletenessContext? context = null)
    {
        ParkDataCompletenessContext scoreContext = context ?? new ParkDataCompletenessContext();
        DataCompletenessScoreBuilder score = new DataCompletenessScoreBuilder();

        score.Add(DataCompletenessScoringRules.HasText(this.Name), 2);
        score.Add(this.IsProjectRelevantForScoring(), 2);
        score.Add(this.AdminReviewStatus != AdminReviewStatus.NotRelevant, 2);
        score.Add(scoreContext.HasNoProbableDuplicate, 2);
        score.Add(scoreContext.HasCleanLegacyDataOrDocumentedDebt, 2);

        score.Add(DataCompletenessScoringRules.HasText(this.CountryCode), 1);
        score.Add(this.IsLocalizable() && DataCompletenessScoringRules.HasText(this.City), 1);
        score.Add(this.IsLocalizable() && (DataCompletenessScoringRules.HasText(this.Street) || DataCompletenessScoringRules.HasText(this.PostalCode)), 1);
        score.AddIfApplicable(this.ExpectsKnownCoordinates(), DataCompletenessScoringRules.HasValidPosition(this.Position), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasValidPosition(this.Position), this.PositionDoesNotLookLikeDefault(), 1);
        score.Add(this.Type.HasValue, 2);
        score.Add(this.Status is ParkStatus.Operating or ParkStatus.ClosedDefinitively, 1);
        score.AddIfApplicable(this.HasDocumentableOpeningDate(), this.OpeningDate.HasValue || DataCompletenessScoringRules.HasText(this.OpeningDateText), 1);
        score.AddIfApplicable(this.Status == ParkStatus.ClosedDefinitively, this.ClosingDate.HasValue || DataCompletenessScoringRules.HasText(this.ClosingDateText), 1);
        score.Add(DataCompletenessScoringRules.HasText(this.WebsiteUrl), 1);

        score.Add(this.AudienceClassification.HasValue, 2);
        score.Add(this.AudienceClassification.HasValue, 1);
        score.Add(this.AudienceClassification.HasValue, 1);

        score.Add(scoreContext.ParkItemsTotalCount > 0, 3);
        score.Add(scoreContext.DistinctParkItemCategoryCount >= 3, 3);
        score.Add(this.HasCoherentParkItemCount(scoreContext.ParkItemsTotalCount), 3);
        score.Add(scoreContext.ParkItemsVisibleCount > 0 && scoreContext.ParkItemsVisibleCount <= scoreContext.ParkItemsTotalCount, 2);
        score.AddIfApplicable(this.Status == ParkStatus.ClosedDefinitively || scoreContext.ClosedImportantParkItemsCount > 0, scoreContext.ClosedImportantParkItemsCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.ParkItemsWithKnownStatusOrDatesCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.AttractionManufacturerIdsCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.AttractionsWithAccessConditionsCount > 0, 2);

        score.AddIfApplicable(scoreContext.HasOfficialZones, scoreContext.ZonesTotalCount > 0, 2);
        score.AddIfApplicable(scoreContext.HasOfficialZones && scoreContext.ParkItemsTotalCount > 0, scoreContext.ParkItemsAttachedToZonesCount > 0, 1);
        score.AddIfApplicable(scoreContext.HasOfficialZones, scoreContext.ZonesWithDescriptionsCount > 0, 1);

        score.Add(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), 4);
        score.AddIfApplicable(this.IsPotentiallyPublishable(), DataCompletenessScoringRules.CountPublicLanguagesWithText(this.Descriptions) == DataCompletenessScoringRules.PublicLanguageCount, 2);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.ParkItemsWithDescriptionsCount > 0, 3);
        score.AddIfApplicable(scoreContext.HasOfficialZones, scoreContext.ZonesWithDescriptionsCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.CommercialOrServiceItemsWithDescriptionsCount > 0, 1);
        score.Add(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), 1);
        score.Add(this.Descriptions.All(static description => !DataCompletenessScoringRules.HasInternalJargon(description.Value)), 1);
        score.Add(this.Descriptions.Any(static description => DataCompletenessScoringRules.HasMeaningfulText(description.Value)), 1);

        score.Add(DataCompletenessScoringRules.HasText(this.CurrentLogoImageId) || scoreContext.ParkPublishedImageCount > 0, 2);
        score.Add(scoreContext.ParkPublishedImageCount > 0, 2);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.ParkItemPublishedImageCount > 0, 2);
        score.AddIfApplicable(scoreContext.ParkPublishedImageCount > 0, scoreContext.ParkImagesWithResolvedOwnerCount == scoreContext.ParkPublishedImageCount, 1);
        score.AddIfApplicable(scoreContext.ParkPublishedImageCount > 0, scoreContext.ParkImagesWithLocalizedAltTextCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkPublishedImageCount > 0, scoreContext.ParkImagesWithResolvedOwnerCount == scoreContext.ParkPublishedImageCount, 1);
        score.AddIfApplicable(scoreContext.HasOriginalMedia, scoreContext.HasOriginalMedia, 1);

        if (this.Status != ParkStatus.ClosedDefinitively)
        {
            score.Add(scoreContext.HasOpeningHours, 2);
            score.Add(scoreContext.HasOpeningHoursSource, 2);
            score.AddIfApplicable(scoreContext.HasOpeningHours, scoreContext.HasOpeningHoursTimeZone, 1);
            score.AddIfApplicable(scoreContext.HasOpeningHoursExceptions, scoreContext.HasOpeningHoursExceptions, 1);
            score.AddIfApplicable(scoreContext.HasOpeningHours, scoreContext.OpeningHoursStatus != ParkOpeningHoursAdminStatus.Expired, 1);
            score.AddIfApplicable(scoreContext.HasOpeningHours, scoreContext.HasOpeningHoursRecentVerification || scoreContext.OpeningHoursStatus == ParkOpeningHoursAdminStatus.UpToDate, 1);
        }

        score.AddIfApplicable(this.RequiresHistoryForScoring(), scoreContext.ParkHistoryEventCount > 0, 3);
        score.AddIfApplicable(scoreContext.ParkHistoryEventCount > 0, scoreContext.MajorHistoryEventCount > 0 && scoreContext.HistoryEventsWithSourcesCount > 0, 2);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.ParkItemHistoryEventCount > 0, 2);
        score.AddIfApplicable(this.RequiresHistoryForScoring(), scoreContext.PublishedArticleCount > 0, 2);
        score.AddIfApplicable(scoreContext.PublishedArticleCount > 0, scoreContext.StructuredArticleCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkHistoryEventCount > 0 || scoreContext.PublishedArticleCount > 0, scoreContext.LocalizedHistoryContentCount > 0, 1);
        score.AddIfApplicable(scoreContext.ParkHistoryEventCount > 0, scoreContext.HistoryEventsWithSourcesCount > 0, 2);
        score.AddIfApplicable(scoreContext.ParkHistoryEventCount > 0, scoreContext.HistoryEventsWithMediaCount > 0, 1);

        score.AddIfApplicable(this.HasDocumentableOperator(), DataCompletenessScoringRules.HasText(this.OperatorId), 1);
        score.AddIfApplicable(this.HasDocumentableFounder(), DataCompletenessScoringRules.HasText(this.FounderId), 1);
        score.AddIfApplicable(scoreContext.ParkItemsTotalCount > 0, scoreContext.AttractionManufacturerIdsCount > 0, 2);
        score.AddIfApplicable(this.HasAnyReference(scoreContext), scoreContext.ImportantReferencesWithDescriptionsCount > 0, 2);
        score.AddIfApplicable(this.HasAnyReference(scoreContext), scoreContext.HasNoProbableDuplicate, 1);
        score.AddIfApplicable(this.HasAnyReference(scoreContext), scoreContext.ReferencesWithUsefulDetailsCount > 0, 1);

        score.AddIfApplicable(this.IsVisible, scoreContext.HasPublicSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasPublicSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasPublicSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasPublicSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, this.IsPotentiallyPublishable(), 1);
        score.AddIfApplicable(this.IsVisible, DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), 1);

        score.Add(scoreContext.HasResolvedAttachmentKeys, 2);
        score.Add(scoreContext.HasNoKnownBlockingWarnings, 1);
        score.Add(scoreContext.HasCriticalSources || !this.RequiresHistoryForScoring(), 2);
        score.Add(scoreContext.HasNoInventedDates, 1);
        score.Add(scoreContext.HasNoKnownBlockingWarnings, 1);
        score.Add(scoreContext.HasStructuredTechnicalDataOnly, 1);
        score.AddIfApplicable(score.HasMissingPoints, scoreContext.HasDocumentedRemainingDebt, 1);
        score.AddIfApplicable(this.AdminReviewStatus == AdminReviewStatus.Validated, this.AdminReviewStatus == AdminReviewStatus.Validated, 1);

        return score.Build();
    }

    private bool HasRelevantDateInformation()
    {
        if (this.OpeningDate.HasValue || DataCompletenessScoringRules.HasText(this.OpeningDateText))
        {
            return true;
        }

        return this.Status == ParkStatus.ClosedDefinitively
            && (this.ClosingDate.HasValue || DataCompletenessScoringRules.HasText(this.ClosingDateText));
    }

    private bool IsProjectRelevantForScoring()
    {
        return this.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && !DataCompletenessScoringRules.IsPlaceholderName(this.Name);
    }

    private bool IsLocalizable()
    {
        return this.Status == ParkStatus.Operating
            || this.Status == ParkStatus.ClosedDefinitively;
    }

    private bool ExpectsKnownCoordinates()
    {
        return this.Status == ParkStatus.Operating
            || this.Status == ParkStatus.ClosedDefinitively;
    }

    private bool PositionDoesNotLookLikeDefault()
    {
        return DataCompletenessScoringRules.HasValidPosition(this.Position);
    }

    private bool HasDocumentableOpeningDate()
    {
        return this.Status == ParkStatus.Operating
            || this.Status == ParkStatus.ClosedDefinitively;
    }

    private bool HasCoherentParkItemCount(int totalCount)
    {
        if (totalCount <= 0)
        {
            return false;
        }

        return this.AudienceClassification switch
        {
            ParkAudienceClassification.International => totalCount >= 10,
            ParkAudienceClassification.National => totalCount >= 6,
            ParkAudienceClassification.Regional => totalCount >= 3,
            _ => totalCount >= 1,
        };
    }

    private bool IsPotentiallyPublishable()
    {
        return this.AdminReviewStatus == AdminReviewStatus.Validated
            && !DataCompletenessScoringRules.IsPlaceholderName(this.Name);
    }

    private bool RequiresHistoryForScoring()
    {
        return this.Status == ParkStatus.ClosedDefinitively
            || this.AudienceClassification is ParkAudienceClassification.International or ParkAudienceClassification.National;
    }

    private bool HasDocumentableOperator()
    {
        return this.Status == ParkStatus.Operating
            || this.Status == ParkStatus.ClosedDefinitively;
    }

    private bool HasDocumentableFounder()
    {
        return this.AudienceClassification is ParkAudienceClassification.International or ParkAudienceClassification.National
            || this.Status == ParkStatus.ClosedDefinitively;
    }

    private bool HasAnyReference(ParkDataCompletenessContext context)
    {
        return DataCompletenessScoringRules.HasText(this.OperatorId)
            || DataCompletenessScoringRules.HasText(this.FounderId)
            || context.AttractionManufacturerIdsCount > 0;
    }
}
