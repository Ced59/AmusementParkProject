namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Type HTTP de contrainte d'accès d'une attraction.
/// </summary>
public enum AttractionAccessConditionTypeDto
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
