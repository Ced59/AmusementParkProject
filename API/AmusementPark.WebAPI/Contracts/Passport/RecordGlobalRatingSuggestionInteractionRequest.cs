using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class RecordGlobalRatingSuggestionInteractionRequest
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public GlobalRatingSuggestionInteractionTypeDto InteractionType { get; init; }
    public DateTime PresentedAtUtc { get; init; }
}
