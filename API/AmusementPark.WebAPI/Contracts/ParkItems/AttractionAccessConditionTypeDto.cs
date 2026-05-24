using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Type HTTP de contrainte d'accès d'une attraction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
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
