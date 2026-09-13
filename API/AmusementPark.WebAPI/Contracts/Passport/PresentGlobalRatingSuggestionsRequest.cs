using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PresentGlobalRatingSuggestionsRequest
{
    public IReadOnlyCollection<GlobalRatingSuggestionPresentationTargetDto> Targets { get; init; } =
        Array.Empty<GlobalRatingSuggestionPresentationTargetDto>();
}
