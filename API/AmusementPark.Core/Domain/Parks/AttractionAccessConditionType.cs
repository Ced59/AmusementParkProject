namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Type de contrainte d'accès d'une attraction.
/// </summary>
public enum AttractionAccessConditionType
{
    MinHeight,
    MinHeightAccompanied,
    MaxHeight,
    MinAge,
    MinAgeAccompanied,
    PregnancyRestriction,
    HeartRestriction,
    BackNeckRestriction,
    WheelchairTransferRequired,
    AccessPassRequired,
    Custom,
}
