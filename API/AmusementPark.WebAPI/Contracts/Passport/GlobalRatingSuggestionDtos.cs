using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Passport;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionTargetTypeDto
{
    Park = 1,
    ParkItem = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionReasonDto
{
    RecentExperiencesLower = 1,
    RecentExperiencesHigher = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GlobalRatingSuggestionInteractionTypeDto
{
    Accepted = 2,
    Dismissed = 3,
}

public sealed class GlobalRatingSuggestionDto
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public string? ParkItemCategory { get; init; }
    public double CurrentGlobalRating { get; init; }
    public double LatestObservationRating { get; init; }
    public double RecentAverage { get; init; }
    public double HistoricalMedian { get; init; }
    public int NewObservationCount { get; init; }
    public int RecentObservationCount { get; init; }
    public GlobalRatingSuggestionReasonDto Reason { get; init; }
    public DateTime LatestObservationAtUtc { get; init; }
}

public sealed class GlobalRatingSuggestionsDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
    public int MinimumNewObservationCount { get; init; }
    public int CooldownDays { get; init; }
    public IReadOnlyCollection<GlobalRatingSuggestionDto> Suggestions { get; init; } =
        Array.Empty<GlobalRatingSuggestionDto>();
}

public sealed class GlobalRatingSuggestionPreferenceDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class SetGlobalRatingSuggestionPreferenceRequest
{
    public bool IsEnabled { get; init; }
}

public sealed class RecordGlobalRatingSuggestionInteractionRequest
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
    public GlobalRatingSuggestionInteractionTypeDto InteractionType { get; init; }
}

public sealed class GlobalRatingSuggestionPresentationTargetDto
{
    public GlobalRatingSuggestionTargetTypeDto TargetType { get; init; }
    public string TargetId { get; init; } = string.Empty;
}

public sealed class PresentGlobalRatingSuggestionsRequest
{
    public IReadOnlyCollection<GlobalRatingSuggestionPresentationTargetDto> Targets { get; init; } =
        Array.Empty<GlobalRatingSuggestionPresentationTargetDto>();
}

public sealed class GlobalRatingSuggestionPresentationDto
{
    public bool IsAvailable { get; init; }
    public bool IsEnabled { get; init; }
    public IReadOnlyCollection<GlobalRatingSuggestionPresentationTargetDto> PresentedTargets { get; init; } =
        Array.Empty<GlobalRatingSuggestionPresentationTargetDto>();
}
