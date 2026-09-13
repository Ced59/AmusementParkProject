using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class GlobalRatingSuggestionPresentationTargetDto
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
}
