using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionTargetTypeDto
{
    Park = 1,
    ParkItem = 2,
}
