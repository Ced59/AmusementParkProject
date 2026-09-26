namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Table explicite de conversion des types historiques antérieurs.
/// Une valeur inconnue n'est jamais rabattue silencieusement vers Other.
/// </summary>
public static class LegacyHistoryEventTypeMapper
{
    public static bool TryMap(
        HistoryEntityType entityType,
        string? legacyEventType,
        out LegacyHistoryEventTypeMapping? mapping)
    {
        if (RequiresManualClassification(entityType, legacyEventType))
        {
            mapping = null;
            return false;
        }

        mapping = entityType switch
        {
            HistoryEntityType.Park => MapPark(legacyEventType),
            HistoryEntityType.ParkItem or HistoryEntityType.StandaloneAttraction =>
                MapParkItem(legacyEventType),
            _ => null,
        };
        return mapping is not null;
    }

    public static bool RequiresManualClassification(
        HistoryEntityType entityType,
        string? legacyEventType)
    {
        string? normalizedEventType = legacyEventType?.Trim();
        return entityType switch
        {
            HistoryEntityType.Park => Enum.TryParse(
                    normalizedEventType,
                    true,
                    out ParkHistoryEventType parkEventType)
                && parkEventType == ParkHistoryEventType.SeasonOpening,
            HistoryEntityType.ParkItem or HistoryEntityType.StandaloneAttraction => Enum.TryParse(
                    normalizedEventType,
                    true,
                    out ParkItemHistoryEventType parkItemEventType)
                && parkItemEventType == ParkItemHistoryEventType.SeasonOpening,
            _ => false,
        };
    }

    private static LegacyHistoryEventTypeMapping? MapPark(string? legacyEventType)
    {
        if (!Enum.TryParse(legacyEventType?.Trim(), true, out ParkHistoryEventType type)
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
            ParkHistoryEventType.OwnershipChange
                or ParkHistoryEventType.Acquisition
                or ParkHistoryEventType.Sale => Attribute(
                    HistoricalFactType.OwnerChange,
                    HistoricalAttributeKind.Owner),
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

    private static LegacyHistoryEventTypeMapping? MapParkItem(string? legacyEventType)
    {
        if (!Enum.TryParse(legacyEventType?.Trim(), true, out ParkItemHistoryEventType type)
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
            ParkItemHistoryEventType.ManufacturerChange => Attribute(
                HistoricalFactType.ManufacturerChange,
                HistoricalAttributeKind.Manufacturer),
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

    private static LegacyHistoryEventTypeMapping Simple(HistoricalFactType factType)
    {
        return new LegacyHistoryEventTypeMapping(factType, null, null, null);
    }

    private static LegacyHistoryEventTypeMapping Lifecycle(
        HistoricalFactType factType,
        LifecycleBoundaryMeaning boundaryMeaning)
    {
        return new LegacyHistoryEventTypeMapping(factType, boundaryMeaning, null, null);
    }

    private static LegacyHistoryEventTypeMapping Attribute(
        HistoricalFactType factType,
        HistoricalAttributeKind attributeKind)
    {
        return new LegacyHistoryEventTypeMapping(
            factType,
            null,
            attributeKind,
            AttributeBoundaryMeaning.Unspecified);
    }
}
