using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class SetGlobalRatingSuggestionPreferenceRequest
{
    public bool IsEnabled { get; init; }
}
