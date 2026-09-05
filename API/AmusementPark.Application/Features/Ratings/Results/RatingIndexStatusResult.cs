namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingIndexStatusResult(
    string Collection,
    string Name,
    bool IsPresent,
    bool IsUnique,
    bool IsHidden,
    bool HasUnexpectedOptions,
    bool SupportsExpectedQueries,
    bool MatchesExpectedDefinition,
    string ExpectedKeys,
    string? ActualKeys);
