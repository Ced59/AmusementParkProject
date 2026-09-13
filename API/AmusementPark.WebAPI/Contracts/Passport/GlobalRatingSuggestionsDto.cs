using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionsDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
    public int MinimumNewObservationCount { get; init; }
    public int CooldownDays { get; init; }
    public IReadOnlyCollection<GlobalRatingSuggestionDto> Suggestions { get; init; } =
        Array.Empty<GlobalRatingSuggestionDto>();
}
