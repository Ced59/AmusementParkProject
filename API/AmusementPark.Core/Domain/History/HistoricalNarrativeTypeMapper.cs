namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Table explicite de projection des types narratifs vers les faits HIST canoniques.
/// Une valeur inconnue n'est jamais rabattue silencieusement vers Other.
/// </summary>
public static class HistoricalNarrativeTypeMapper
{
    public static bool TryMap(
        HistoryEntityType entityType,
        string? eventType,
        out HistoricalNarrativeTypeMapping? mapping)
    {
        if (RequiresManualClassification(entityType, eventType))
        {
            mapping = null;
            return false;
        }

        mapping = entityType switch
        {
            HistoryEntityType.Park => MapPark(eventType),
            HistoryEntityType.ParkItem or HistoryEntityType.StandaloneAttraction =>
                MapParkItem(eventType),
            _ => null,
        };
        return mapping is not null;
    }

    public static bool RequiresManualClassification(
        HistoryEntityType entityType,
        string? eventType)
    {
        string? normalizedEventType = eventType?.Trim();
        return entityType switch
        {
            HistoryEntityType.Park => Enum.TryParse(
                    normalizedEventType,
                    true,
                    out ParkHistoryEventType parkEventType)
                && parkEventType is ParkHistoryEventType.SeasonOpening
                    or ParkHistoryEventType.OwnershipChange
                    or ParkHistoryEventType.Acquisition
                    or ParkHistoryEventType.Sale,
            HistoryEntityType.ParkItem or HistoryEntityType.StandaloneAttraction => Enum.TryParse(
                    normalizedEventType,
                    true,
                    out ParkItemHistoryEventType parkItemEventType)
                && parkItemEventType is ParkItemHistoryEventType.SeasonOpening
                    or ParkItemHistoryEventType.ManufacturerChange,
            _ => false,
        };
    }

    private static HistoricalNarrativeTypeMapping? MapPark(string? eventType)
    {
        if (!Enum.TryParse(eventType?.Trim(), true, out ParkHistoryEventType type)
            || !Enum.IsDefined(type))
        {
            return null;
        }

        return type switch
        {
            ParkHistoryEventType.Announcement => Simple(HistoricalFactType.Announcement),
            ParkHistoryEventType.ConstructionStart
                or ParkHistoryEventType.ConstructionMilestone
                or ParkHistoryEventType.Masterplan
                or ParkHistoryEventType.InfrastructureChange
                or ParkHistoryEventType.TransportChange
                or ParkHistoryEventType.MaintenanceCampaign => Simple(HistoricalFactType.Construction),
            ParkHistoryEventType.Opening => Lifecycle(
                    HistoricalFactType.Opening,
                    LifecycleBoundaryMeaning.FirstOperatingDay),
            ParkHistoryEventType.Closure => Lifecycle(
                HistoricalFactType.Closure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkHistoryEventType.Reopening => Lifecycle(
                HistoricalFactType.Reopening,
                LifecycleBoundaryMeaning.FirstOperatingDay),
            ParkHistoryEventType.TemporaryClosure => Lifecycle(
                HistoricalFactType.TemporaryClosure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkHistoryEventType.DefinitiveClosure => Lifecycle(
                HistoricalFactType.DefinitiveClosure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkHistoryEventType.Rename => Attribute(
                HistoricalFactType.Renaming,
                HistoricalAttributeKind.Name),
            ParkHistoryEventType.BrandingChange => Attribute(
                HistoricalFactType.PositioningChange,
                HistoricalAttributeKind.MarketPositioning),
            ParkHistoryEventType.LogoChange => Attribute(
                HistoricalFactType.LogoChange,
                HistoricalAttributeKind.Logo),
            ParkHistoryEventType.OperatorChange => Attribute(
                HistoricalFactType.OperatorChange,
                HistoricalAttributeKind.Operator),
            ParkHistoryEventType.Expansion
                or ParkHistoryEventType.ResortExpansion
                or ParkHistoryEventType.Redevelopment => Simple(HistoricalFactType.Extension),
            ParkHistoryEventType.AreaOpening => Simple(HistoricalFactType.ZoneCreation),
            ParkHistoryEventType.Demolition => Simple(HistoricalFactType.Reduction),
            ParkHistoryEventType.Other => Simple(HistoricalFactType.Other),
            ParkHistoryEventType.Foundation
                or ParkHistoryEventType.AttractionOpening
                or ParkHistoryEventType.AttractionClosure
                or ParkHistoryEventType.FounderMilestone
                or ParkHistoryEventType.Bankruptcy
                or ParkHistoryEventType.Liquidation
                or ParkHistoryEventType.LegalDispute
                or ParkHistoryEventType.Investment
                or ParkHistoryEventType.HotelOpening
                or ParkHistoryEventType.ThemedAreaChange
                or ParkHistoryEventType.ParadeOrShowLaunch
                or ParkHistoryEventType.FestivalLaunch
                or ParkHistoryEventType.RecordOrAward
                or ParkHistoryEventType.AttendanceMilestone
                or ParkHistoryEventType.SafetyIncident
                or ParkHistoryEventType.Accident
                or ParkHistoryEventType.OperationalIncident
                or ParkHistoryEventType.WeatherEvent
                or ParkHistoryEventType.Fire
                or ParkHistoryEventType.Flood
                or ParkHistoryEventType.StormDamage
                or ParkHistoryEventType.HealthCrisis
                or ParkHistoryEventType.SecurityEvent
                or ParkHistoryEventType.StrikeOrSocialMovement
                or ParkHistoryEventType.RegulatoryChange
                or ParkHistoryEventType.PreservationOrHeritage
                or ParkHistoryEventType.TechnologyChange
                or ParkHistoryEventType.SustainabilityChange
                or ParkHistoryEventType.GuestExperienceChange
                or ParkHistoryEventType.PricingOrTicketingChange
                or ParkHistoryEventType.Partnership
                or ParkHistoryEventType.MediaAppearance => Simple(HistoricalFactType.MajorEvent),
            _ => null,
        };
    }

    private static HistoricalNarrativeTypeMapping? MapParkItem(string? eventType)
    {
        if (!Enum.TryParse(eventType?.Trim(), true, out ParkItemHistoryEventType type)
            || !Enum.IsDefined(type))
        {
            return null;
        }

        return type switch
        {
            ParkItemHistoryEventType.Announcement => Simple(HistoricalFactType.Announcement),
            ParkItemHistoryEventType.DesignStart
                or ParkItemHistoryEventType.ConstructionStart
                or ParkItemHistoryEventType.ConstructionMilestone
                or ParkItemHistoryEventType.TestingStart => Simple(HistoricalFactType.Construction),
            ParkItemHistoryEventType.SoftOpening
                or ParkItemHistoryEventType.Opening => Lifecycle(
                    HistoricalFactType.Opening,
                    LifecycleBoundaryMeaning.FirstOperatingDay),
            ParkItemHistoryEventType.Closure => Lifecycle(
                HistoricalFactType.Closure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkItemHistoryEventType.TemporaryClosure => Lifecycle(
                HistoricalFactType.TemporaryClosure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkItemHistoryEventType.DefinitiveClosure => Lifecycle(
                HistoricalFactType.DefinitiveClosure,
                LifecycleBoundaryMeaning.Unspecified),
            ParkItemHistoryEventType.Reopening => Lifecycle(
                HistoricalFactType.Reopening,
                LifecycleBoundaryMeaning.FirstOperatingDay),
            ParkItemHistoryEventType.Rename => Attribute(
                HistoricalFactType.Renaming,
                HistoricalAttributeKind.Name),
            ParkItemHistoryEventType.ThemeChange
                or ParkItemHistoryEventType.StoryChange => Attribute(
                    HistoricalFactType.Retheming,
                    HistoricalAttributeKind.Theme),
            ParkItemHistoryEventType.LogoChange => Attribute(
                HistoricalFactType.LogoChange,
                HistoricalAttributeKind.Logo),
            ParkItemHistoryEventType.RelocationDeparture
                or ParkItemHistoryEventType.RelocationArrival
                or ParkItemHistoryEventType.Transfer
                or ParkItemHistoryEventType.Reinstallation => Attribute(
                    HistoricalFactType.Relocation,
                    HistoricalAttributeKind.Location),
            ParkItemHistoryEventType.Dismantling
                or ParkItemHistoryEventType.Demolition => Simple(HistoricalFactType.Dismantling),
            ParkItemHistoryEventType.Replacement => Simple(HistoricalFactType.Replacement),
            ParkItemHistoryEventType.Refurbishment
                or ParkItemHistoryEventType.Rehab
                or ParkItemHistoryEventType.Retrack
                or ParkItemHistoryEventType.LayoutChange
                or ParkItemHistoryEventType.RideSystemChange
                or ParkItemHistoryEventType.CapacityChange
                or ParkItemHistoryEventType.TrainChange
                or ParkItemHistoryEventType.VehicleChange
                or ParkItemHistoryEventType.RestraintChange
                or ParkItemHistoryEventType.ModelChange
                or ParkItemHistoryEventType.AccessibilityChange
                or ParkItemHistoryEventType.HeightRequirementChange
                or ParkItemHistoryEventType.QueueChange
                or ParkItemHistoryEventType.FastPassChange
                or ParkItemHistoryEventType.SafetyModification
                or ParkItemHistoryEventType.TechnicalFailure
                or ParkItemHistoryEventType.OperationalChange =>
                    Simple(HistoricalFactType.TechnicalModification),
            ParkItemHistoryEventType.Other => Simple(HistoricalFactType.Other),
            ParkItemHistoryEventType.SponsorChange
                or ParkItemHistoryEventType.Storage
                or ParkItemHistoryEventType.Sale
                or ParkItemHistoryEventType.Acquisition
                or ParkItemHistoryEventType.Accident
                or ParkItemHistoryEventType.Incident
                or ParkItemHistoryEventType.Fire
                or ParkItemHistoryEventType.WeatherDamage
                or ParkItemHistoryEventType.RecordOrAward
                or ParkItemHistoryEventType.MediaAppearance
                or ParkItemHistoryEventType.PreservationOrHeritage =>
                    Simple(HistoricalFactType.MajorEvent),
            _ => null,
        };
    }

    private static HistoricalNarrativeTypeMapping Simple(HistoricalFactType factType)
    {
        return new HistoricalNarrativeTypeMapping(factType, null, null, null);
    }

    private static HistoricalNarrativeTypeMapping Lifecycle(
        HistoricalFactType factType,
        LifecycleBoundaryMeaning boundaryMeaning)
    {
        return new HistoricalNarrativeTypeMapping(factType, boundaryMeaning, null, null);
    }

    private static HistoricalNarrativeTypeMapping Attribute(
        HistoricalFactType factType,
        HistoricalAttributeKind attributeKind)
    {
        return new HistoricalNarrativeTypeMapping(
            factType,
            null,
            attributeKind,
            AttributeBoundaryMeaning.Unspecified);
    }
}
