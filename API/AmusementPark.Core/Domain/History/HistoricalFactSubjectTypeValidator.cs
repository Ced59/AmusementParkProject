namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Empêche qu'une famille de fait soit attachée à un sujet incompatible.
/// </summary>
public static class HistoricalFactSubjectTypeValidator
{
    public static void Validate(HistoricalSubjectType subjectType, HistoricalFactType factType)
    {
        if (!IsCompatible(subjectType, factType))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "The historical fact type is incompatible with its subject type.");
        }
    }

    private static bool IsCompatible(HistoricalSubjectType subjectType, HistoricalFactType factType)
    {
        return subjectType switch
        {
            HistoricalSubjectType.Park => IsParkFact(factType),
            HistoricalSubjectType.ParkItem or HistoricalSubjectType.StandaloneAttraction =>
                IsAttractionFact(factType),
            HistoricalSubjectType.ParkZone => IsZoneFact(factType),
            HistoricalSubjectType.ParkOperator or HistoricalSubjectType.AttractionManufacturer =>
                IsOrganizationFact(factType),
            _ => false,
        };
    }

    private static bool IsParkFact(HistoricalFactType factType)
    {
        return factType is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.Renaming
            or HistoricalFactType.OperatorChange
            or HistoricalFactType.OwnerChange
            or HistoricalFactType.Extension
            or HistoricalFactType.Reduction
            or HistoricalFactType.ZoneCreation
            or HistoricalFactType.ZoneRemoval
            or HistoricalFactType.MajorEvent
            or HistoricalFactType.PositioningChange
            or HistoricalFactType.Announcement
            or HistoricalFactType.Construction
            or HistoricalFactType.LogoChange
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure
            or HistoricalFactType.Other;
    }

    private static bool IsAttractionFact(HistoricalFactType factType)
    {
        return factType is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.Renaming
            or HistoricalFactType.MajorEvent
            or HistoricalFactType.Announcement
            or HistoricalFactType.Construction
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure
            or HistoricalFactType.Dismantling
            or HistoricalFactType.Relocation
            or HistoricalFactType.Retheming
            or HistoricalFactType.ManufacturerChange
            or HistoricalFactType.OperatorChange
            or HistoricalFactType.TechnicalModification
            or HistoricalFactType.Replacement
            or HistoricalFactType.ZoneMove
            or HistoricalFactType.LogoChange
            or HistoricalFactType.Other;
    }

    private static bool IsZoneFact(HistoricalFactType factType)
    {
        return factType is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.Renaming
            or HistoricalFactType.MajorEvent
            or HistoricalFactType.Announcement
            or HistoricalFactType.Construction
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure
            or HistoricalFactType.ZoneRenaming
            or HistoricalFactType.Retheming
            or HistoricalFactType.LogoChange
            or HistoricalFactType.Other;
    }

    private static bool IsOrganizationFact(HistoricalFactType factType)
    {
        return factType is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.Renaming
            or HistoricalFactType.OwnerChange
            or HistoricalFactType.MajorEvent
            or HistoricalFactType.PositioningChange
            or HistoricalFactType.Announcement
            or HistoricalFactType.Other;
    }
}
