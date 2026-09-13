using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionPresentedTargetDto
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public DateTime PresentedAtUtc { get; init; }
}
