using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Élément fonctionnel d'un parc.
/// </summary>
public sealed class ParkItem : GeolocatedEntityBase
{
    /// <summary>
    /// Identifiant du parc parent.
    /// </summary>
    public string ParkId { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant éventuel de la zone parent.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Nom d'affichage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Catégorie métier globale.
    /// </summary>
    public ParkItemCategory Category { get; set; }

    /// <summary>
    /// Type métier détaillé.
    /// </summary>
    public ParkItemType Type { get; set; }

    /// <summary>
    /// Sous-type libre éventuel.
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Détails spécifiques aux attractions.
    /// </summary>
    public AttractionDetails? AttractionDetails { get; set; }

    /// <summary>
    /// Localisations fonctionnelles spécifiques à l'attraction.
    /// </summary>
    public AttractionLocations? AttractionLocations { get; set; }

    /// <summary>
    /// Indique si l'élément est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Statut de traitement interne pour les listes d'administration.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    public DataCompletenessScore CalculateDataCompletenessScore(ParkItemDataCompletenessContext? context = null)
    {
        ParkItemDataCompletenessContext scoreContext = context ?? new ParkItemDataCompletenessContext();
        DataCompletenessScoreBuilder score = new DataCompletenessScoreBuilder();

        score.Add(DataCompletenessScoringRules.HasText(this.Name), 2);
        score.Add(!DataCompletenessScoringRules.IsPlaceholderName(this.Name), 2);
        score.Add(scoreContext.ParentParkResolved && DataCompletenessScoringRules.HasText(this.ParkId), 2);
        score.Add(this.AdminReviewStatus != AdminReviewStatus.NotRelevant, 1);
        score.Add(!this.IsVisible || scoreContext.ParentParkVisible, 1);
        score.Add(scoreContext.HasNoDuplicateInPark, 2);

        score.Add(this.Category != ParkItemCategory.Other, 2);
        score.Add(this.HasPreciseType(), 2);
        score.Add(this.HasOperationalStatus(), 2);
        score.AddIfApplicable(this.HasDocumentableDates(), this.HasDateInformation(), 2);
        score.AddIfApplicable(this.IsClosedForScoring(), this.HasDateInformation(), 1);
        score.Add(!this.HasSeasonalStatusAsPermanentStatus(), 1);
        score.Add(this.HasSourceOrPresenceProof(scoreContext), 2);

        score.AddIfApplicable(scoreContext.HasOfficialZoneContext, DataCompletenessScoringRules.HasText(this.ZoneId), 2);
        score.AddIfApplicable(this.HasUsefulPreciseLocation(), DataCompletenessScoringRules.HasValidPosition(this.Position), 2);
        score.Add(scoreContext.HasNoUnresolvedReferences && scoreContext.ParentParkResolved, 2);
        score.AddIfApplicable(scoreContext.HasUsefulVisitGrouping, scoreContext.HasUsefulVisitGrouping, 1);
        score.Add(scoreContext.HasNoUnresolvedReferences, 1);

        score.AddIfApplicable(this.IsMechanicalAttraction(), DataCompletenessScoringRules.HasText(this.AttractionDetails?.ManufacturerId), 2);
        score.AddIfApplicable(this.IsMechanicalAttraction(), DataCompletenessScoringRules.HasText(this.AttractionDetails?.Model), 2);
        score.AddIfApplicable(this.HasAttractionDetailsForScoring(), this.HasTechnicalDetails(), 2);
        score.Add(this.HasPreciseType(), 2);
        score.AddIfApplicable(this.IsServiceLike(), this.HasServiceDetails(), 2);
        score.AddIfApplicable(this.IsCommercialLike(), this.HasCommercialDetails(), 2);
        score.AddIfApplicable(this.Category == ParkItemCategory.Hotel, this.HasCommercialDetails(), 2);
        score.AddIfApplicable(this.HasAttractionDetailsForScoring(), DataCompletenessScoringRules.HasText(this.AttractionDetails?.SourceUrl), 2);

        if (this.HasAccessConditionsApplicability())
        {
            score.Add(this.AttractionDetails?.AccessConditions.Count > 0, 2);
            score.Add(this.HasMinimumHeightCondition(), 2);
            score.Add(this.HasAccompanimentOrAgeCondition(), 2);
            score.Add(this.HasHealthOrTransferCondition(), 2);
            score.Add(this.HasStructuredAccessConditions(), 2);
        }

        score.AddIfApplicable(this.IsPotentiallyPublic(), DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), 4);
        score.AddIfApplicable(this.IsVisible, DataCompletenessScoringRules.CountPublicLanguagesWithText(this.Descriptions) == DataCompletenessScoringRules.PublicLanguageCount, 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), this.HasTypeAdaptedDescription(), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), this.Descriptions.All(static description => !DataCompletenessScoringRules.HasInternalJargon(description.Value)), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), this.Descriptions.All(static description => !DataCompletenessScoringRules.HasInternalJargon(description.Value)), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions), this.Descriptions.Any(static description => DataCompletenessScoringRules.HasMeaningfulText(description.Value)), 2);

        score.AddIfApplicable(this.IsVisible, scoreContext.HasRepresentativeImage, 2);
        score.AddIfApplicable(scoreContext.HasRepresentativeImage, scoreContext.HasResolvedImageOwner, 1);
        score.AddIfApplicable(scoreContext.HasRepresentativeImage, scoreContext.HasLocalizedImageAltText, 1);
        score.AddIfApplicable(scoreContext.HasRepresentativeImage, scoreContext.HasNonMisleadingImage, 2);
        score.AddIfApplicable(scoreContext.HasOriginalMedia, scoreContext.HasOriginalMedia, 1);
        score.AddIfApplicable(this.IsClosedForScoring() && scoreContext.HasRepresentativeImage, scoreContext.HasHistoricalImageContext, 1);

        score.AddIfApplicable(this.RequiresHistoryForScoring(), scoreContext.HistoryEventCount > 0, 2);
        score.AddIfApplicable(this.RequiresHistoryForScoring(), scoreContext.ClosureOrChangeHistoryEventCount > 0, 2);
        score.AddIfApplicable(scoreContext.HistoryEventCount > 0, scoreContext.HasTimelineConsistentWithParent, 2);
        score.AddIfApplicable(this.RequiresArticleForScoring(), scoreContext.PublishedArticleCount > 0, 2);
        score.AddIfApplicable(scoreContext.HistoryEventCount > 0 || scoreContext.PublishedArticleCount > 0, scoreContext.HistoryEventsWithSourcesCount > 0, 2);

        score.AddIfApplicable(this.IsMechanicalAttraction(), DataCompletenessScoringRules.HasText(this.AttractionDetails?.ManufacturerId), 2);
        score.AddIfApplicable(DataCompletenessScoringRules.HasText(this.AttractionDetails?.ManufacturerId), scoreContext.HasReferenceDetailsOrDocumentedDebt, 1);
        score.AddIfApplicable(scoreContext.HistoryEventCount > 0, scoreContext.ClosureOrChangeHistoryEventCount > 0, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasInternalLinks, 1);
        score.Add(scoreContext.HasNoDuplicateReferences, 1);

        score.AddIfApplicable(this.IsVisible, scoreContext.HasSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasSeoSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.ParentParkVisible, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasNoPlaceholderPublicPage && !DataCompletenessScoringRules.IsPlaceholderName(this.Name), 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasStructuredDataSignals, 1);
        score.AddIfApplicable(this.IsVisible, scoreContext.HasHumanReviewOrDocumentedDebt || this.AdminReviewStatus == AdminReviewStatus.Validated, 1);

        return score.Build();
    }

    private bool HasPreciseType()
    {
        return this.Type != ParkItemType.Other
            && (this.Category != ParkItemCategory.Attraction || this.Type != ParkItemType.Attraction);
    }

    private bool HasOperationalStatus()
    {
        return this.Category != ParkItemCategory.Attraction
            || DataCompletenessScoringRules.HasText(this.AttractionDetails?.Status);
    }

    private bool HasDocumentableDates()
    {
        return this.Category == ParkItemCategory.Attraction;
    }

    private bool HasDateInformation()
    {
        return this.AttractionDetails?.OpeningDate is not null
            || this.AttractionDetails?.ClosingDate is not null
            || DataCompletenessScoringRules.HasText(this.AttractionDetails?.OpeningDateText)
            || DataCompletenessScoringRules.HasText(this.AttractionDetails?.ClosingDateText);
    }

    private bool IsClosedForScoring()
    {
        string status = this.AttractionDetails?.Status ?? string.Empty;
        return status.Contains("closed", StringComparison.OrdinalIgnoreCase)
            || status.Contains("ferme", StringComparison.OrdinalIgnoreCase)
            || status.Contains("fermé", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasSeasonalStatusAsPermanentStatus()
    {
        string status = this.AttractionDetails?.Status ?? string.Empty;
        return status.Contains("seasonal", StringComparison.OrdinalIgnoreCase)
            && status.Contains("permanent", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasSourceOrPresenceProof(ParkItemDataCompletenessContext context)
    {
        return DataCompletenessScoringRules.HasText(this.AttractionDetails?.SourceUrl)
            || context.HasRepresentativeImage
            || context.HistoryEventCount > 0
            || DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions);
    }

    private bool HasUsefulPreciseLocation()
    {
        return this.Category is ParkItemCategory.Attraction or ParkItemCategory.Restaurant or ParkItemCategory.Shop or ParkItemCategory.Service;
    }

    private bool IsMechanicalAttraction()
    {
        return this.Category == ParkItemCategory.Attraction
            && this.Type is ParkItemType.RollerCoaster
                or ParkItemType.WaterRide
                or ParkItemType.FlatRide
                or ParkItemType.DarkRide
                or ParkItemType.FamilyRide
                or ParkItemType.ThrillRide
                or ParkItemType.TransportRide
                or ParkItemType.ObservationRide
                or ParkItemType.DropTower;
    }

    private bool HasAttractionDetailsForScoring()
    {
        return this.Category == ParkItemCategory.Attraction;
    }

    private bool HasTechnicalDetails()
    {
        AttractionDetails? details = this.AttractionDetails;
        return details is not null
            && (DataCompletenessScoringRules.HasText(details.MaterialType)
                || DataCompletenessScoringRules.HasText(details.SeatingType)
                || DataCompletenessScoringRules.HasText(details.LaunchType)
                || details.HeightInMeters.HasValue
                || details.LengthInMeters.HasValue
                || details.SpeedInKmH.HasValue
                || details.CapacityPerHour.HasValue);
    }

    private bool IsServiceLike()
    {
        return this.Category is ParkItemCategory.Service or ParkItemCategory.Transport
            || this.Type is ParkItemType.Service or ParkItemType.Toilets or ParkItemType.FirstAid or ParkItemType.Information or ParkItemType.Locker or ParkItemType.Parking;
    }

    private bool HasServiceDetails()
    {
        return this.HasPreciseType()
            || DataCompletenessScoringRules.HasText(this.Subtype)
            || DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions);
    }

    private bool IsCommercialLike()
    {
        return this.Category is ParkItemCategory.Restaurant or ParkItemCategory.Shop or ParkItemCategory.Hotel;
    }

    private bool HasCommercialDetails()
    {
        return this.HasPreciseType()
            || DataCompletenessScoringRules.HasText(this.Subtype)
            || DataCompletenessScoringRules.HasAnyLocalizedText(this.Descriptions);
    }

    private bool HasAccessConditionsApplicability()
    {
        return this.Category == ParkItemCategory.Attraction;
    }

    private bool HasMinimumHeightCondition()
    {
        return this.AttractionDetails?.AccessConditions.Any(static condition =>
            condition.Type is AttractionAccessConditionType.MinHeight or AttractionAccessConditionType.MinHeightAccompanied
            || condition.TypeKey?.Contains("height", StringComparison.OrdinalIgnoreCase) == true
            || condition.CustomTypeKey?.Contains("height", StringComparison.OrdinalIgnoreCase) == true) == true;
    }

    private bool HasAccompanimentOrAgeCondition()
    {
        return this.AttractionDetails?.AccessConditions.Any(static condition =>
            condition.RequiresAccompaniment == true
            || condition.MinimumCompanionAge.HasValue
            || condition.Type is AttractionAccessConditionType.MinAge or AttractionAccessConditionType.MinAgeAccompanied
            || condition.TypeKey?.Contains("age", StringComparison.OrdinalIgnoreCase) == true) == true;
    }

    private bool HasHealthOrTransferCondition()
    {
        return this.AttractionDetails?.AccessConditions.Any(static condition =>
            condition.Type is AttractionAccessConditionType.HeartRestriction
                or AttractionAccessConditionType.BackNeckRestriction
                or AttractionAccessConditionType.PregnancyRestriction
                or AttractionAccessConditionType.WheelchairTransferRequired
            || condition.TypeKey?.Contains("health", StringComparison.OrdinalIgnoreCase) == true
            || condition.TypeKey?.Contains("pregnan", StringComparison.OrdinalIgnoreCase) == true
            || condition.TypeKey?.Contains("transfer", StringComparison.OrdinalIgnoreCase) == true) == true;
    }

    private bool HasStructuredAccessConditions()
    {
        return this.AttractionDetails?.AccessConditions.Count > 0
            && this.AttractionDetails.AccessConditions.All(static condition =>
                condition.Type != AttractionAccessConditionType.Custom
                || DataCompletenessScoringRules.HasText(condition.TypeKey)
                || DataCompletenessScoringRules.HasText(condition.CustomTypeKey)
                || DataCompletenessScoringRules.HasAnyLocalizedText(condition.CustomTypeLabel));
    }

    private bool IsPotentiallyPublic()
    {
        return this.IsVisible || this.AdminReviewStatus == AdminReviewStatus.Validated;
    }

    private bool HasTypeAdaptedDescription()
    {
        return this.Descriptions.Any(description => DataCompletenessScoringRules.HasMeaningfulText(description.Value, this.Category == ParkItemCategory.Service ? 20 : 40));
    }

    private bool RequiresHistoryForScoring()
    {
        return this.IsClosedForScoring()
            || this.Type is ParkItemType.RollerCoaster
                or ParkItemType.WaterRide
                or ParkItemType.DarkRide
                or ParkItemType.DropTower;
    }

    private bool RequiresArticleForScoring()
    {
        return this.RequiresHistoryForScoring()
            && this.AdminReviewStatus == AdminReviewStatus.Validated;
    }
}
