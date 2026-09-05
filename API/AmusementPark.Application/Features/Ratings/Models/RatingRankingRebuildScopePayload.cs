using System.Text.Json.Serialization;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingRebuildScopePayload(
    [property: JsonPropertyName("scopeKey")] string ScopeKey,
    [property: JsonPropertyName("requestedSourceRevision")] long RequestedSourceRevision,
    [property: JsonPropertyName("methodologyVersion")] string MethodologyVersion,
    [property: JsonPropertyName("forceRebuild")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool ForceRebuild = false);
