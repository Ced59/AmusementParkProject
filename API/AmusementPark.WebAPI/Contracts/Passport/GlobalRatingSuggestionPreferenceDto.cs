using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionPreferenceDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
}
