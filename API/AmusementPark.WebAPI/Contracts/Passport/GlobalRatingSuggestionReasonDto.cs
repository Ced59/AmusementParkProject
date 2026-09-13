using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionReasonDto
{
    RecentExperiencesLower = 1,
    RecentExperiencesHigher = 2,
}
