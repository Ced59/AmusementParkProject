using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionPresentationDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
    public IReadOnlyCollection<GlobalRatingSuggestionPresentedTargetDto> PresentedTargets { get; init; } =
        Array.Empty<GlobalRatingSuggestionPresentedTargetDto>();
}
