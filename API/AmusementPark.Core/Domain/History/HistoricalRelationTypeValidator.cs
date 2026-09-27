namespace AmusementPark.Core.Domain.History;

public static class HistoricalRelationTypeValidator
{
    public static void Validate(
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalRelationDirection direction)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (!Enum.IsDefined(type) || !Enum.IsDefined(direction))
        {
            throw Invalid("A historical relation requires a valid type and direction.");
        }

        if (source.Type == target.Type
            && string.Equals(source.Id, target.Id, StringComparison.Ordinal)
            && string.Equals(source.ContextParkId, target.ContextParkId, StringComparison.Ordinal))
        {
            throw Invalid("A historical subject cannot be related to itself.");
        }

        HistoricalRelationDirection expectedDirection = type is HistoricalRelationType.SamePhysicalAssetAs
            or HistoricalRelationType.SharesLocationWith
                ? HistoricalRelationDirection.Symmetric
                : HistoricalRelationDirection.Directed;
        if (direction != expectedDirection || !AreSubjectsCompatible(source.Type, target.Type, type))
        {
            throw Invalid("The relation direction or subject families are incompatible with its type.");
        }

        if (direction == HistoricalRelationDirection.Symmetric
            && CompareSubjects(source, target) > 0)
        {
            throw Invalid("A symmetric historical relation must use the canonical subject order.");
        }
    }

    private static int CompareSubjects(HistoricalSubject left, HistoricalSubject right)
    {
        int typeComparison = left.Type.CompareTo(right.Type);
        if (typeComparison != 0)
        {
            return typeComparison;
        }

        int idComparison = string.Compare(left.Id, right.Id, StringComparison.Ordinal);
        return idComparison != 0
            ? idComparison
            : string.Compare(left.ContextParkId, right.ContextParkId, StringComparison.Ordinal);
    }

    private static bool AreSubjectsCompatible(
        HistoricalSubjectType source,
        HistoricalSubjectType target,
        HistoricalRelationType type)
    {
        bool sameIdentityFamily = source == target
            && source is HistoricalSubjectType.Park
                or HistoricalSubjectType.ParkItem
                or HistoricalSubjectType.StandaloneAttraction
                or HistoricalSubjectType.ParkZone
                or HistoricalSubjectType.ParkOperator;
        bool bothAttractions = source is HistoricalSubjectType.ParkItem or HistoricalSubjectType.StandaloneAttraction
            && target is HistoricalSubjectType.ParkItem or HistoricalSubjectType.StandaloneAttraction;
        return type switch
        {
            HistoricalRelationType.RenamedTo => sameIdentityFamily,
            HistoricalRelationType.ReplacedBy => bothAttractions,
            HistoricalRelationType.MovedTo => bothAttractions,
            HistoricalRelationType.RethemedAs => bothAttractions,
            HistoricalRelationType.SuccessorOf => sameIdentityFamily || bothAttractions,
            HistoricalRelationType.SamePhysicalAssetAs => bothAttractions,
            HistoricalRelationType.SharesLocationWith => bothAttractions,
            HistoricalRelationType.OperatedByDuring => source is HistoricalSubjectType.Park
                    or HistoricalSubjectType.ParkItem
                    or HistoricalSubjectType.StandaloneAttraction
                && target == HistoricalSubjectType.ParkOperator,
            HistoricalRelationType.LocatedInZoneDuring => source == HistoricalSubjectType.ParkItem
                && target == HistoricalSubjectType.ParkZone,
            _ => false,
        };
    }

    private static HistoricalPersistenceValidationException Invalid(string message)
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidRelation,
            message);
    }
}
