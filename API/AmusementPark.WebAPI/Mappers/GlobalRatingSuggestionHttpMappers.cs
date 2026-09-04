using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class GlobalRatingSuggestionHttpMappers
{
    public static GlobalRatingSuggestionsDto ToHttp(
        this GlobalRatingSuggestionsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new GlobalRatingSuggestionsDto
        {
            IsAvailable = result.IsAvailable,
            IsEnabled = result.IsEnabled,
            MinimumNewObservationCount = result.MinimumNewObservationCount,
            CooldownDays = result.CooldownDays,
            Suggestions = result.Suggestions.Select(static suggestion =>
                new GlobalRatingSuggestionDto
                {
                    TargetType = (GlobalRatingSuggestionTargetTypeDto)suggestion.TargetType,
                    TargetId = suggestion.TargetId,
                    TargetName = suggestion.TargetName,
                    ParkId = suggestion.ParkId,
                    ParkName = suggestion.ParkName,
                    ParkItemCategory = suggestion.ParkItemCategory?.ToString(),
                    CurrentGlobalRating = suggestion.CurrentGlobalRating,
                    LatestObservationRating = suggestion.LatestObservationRating,
                    RecentAverage = suggestion.RecentAverage,
                    HistoricalMedian = suggestion.HistoricalMedian,
                    NewObservationCount = suggestion.NewObservationCount,
                    RecentObservationCount = suggestion.RecentObservationCount,
                    Reason = (GlobalRatingSuggestionReasonDto)suggestion.Reason,
                    LatestObservationAtUtc = suggestion.LatestObservationAtUtc,
                }).ToArray(),
        };
    }

    public static GlobalRatingSuggestionPreferenceDto ToHttp(
        this GlobalRatingSuggestionPreferenceResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new GlobalRatingSuggestionPreferenceDto
        {
            IsAvailable = result.IsAvailable,
            IsEnabled = result.IsEnabled,
        };
    }

    public static GlobalRatingSuggestionPresentationDto ToHttp(
        this GlobalRatingSuggestionPresentationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new GlobalRatingSuggestionPresentationDto
        {
            IsAvailable = result.IsAvailable,
            IsEnabled = result.IsEnabled,
            PresentedTargets = result.PresentedTargets.Select(static target =>
                new GlobalRatingSuggestionPresentedTargetDto
                {
                    TargetType = (GlobalRatingSuggestionTargetTypeDto)target.TargetType,
                    TargetId = target.TargetId,
                    PresentedAtUtc = target.PresentedAtUtc,
                }).ToArray(),
        };
    }

    public static RatingTargetType ToDomain(
        this GlobalRatingSuggestionTargetTypeDto targetType)
    {
        return (RatingTargetType)targetType;
    }

    public static GlobalRatingSuggestionInteractionType ToDomain(
        this GlobalRatingSuggestionInteractionTypeDto interactionType)
    {
        return (GlobalRatingSuggestionInteractionType)interactionType;
    }
}
