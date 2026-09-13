using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionInteractionTypeDto
{
    Accepted = 2,
    Dismissed = 3,
}
