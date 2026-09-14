namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Anomalie empêchant d'interpréter la règle d'une condition d'accès.
/// </summary>
public enum AttractionAccessConditionSemanticIssue
{
    UnsupportedType,
    MissingValue,
    InvalidValue,
    MissingUnit,
    InvalidUnit,
    MissingCustomDefinition,
    InconsistentAccompaniment,
    InvalidCompanionAge,
}
