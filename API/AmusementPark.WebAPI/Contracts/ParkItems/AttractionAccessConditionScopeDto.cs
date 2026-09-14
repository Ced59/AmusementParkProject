using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttractionAccessConditionScopeDto
{
    Attraction,
    Vehicle,
    Seat,
    OperatingPeriod,
    Custom,
}
