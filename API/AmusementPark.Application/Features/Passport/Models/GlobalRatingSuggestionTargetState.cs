using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record GlobalRatingSuggestionTargetState(
    RatingTargetType TargetType,
    string TargetId,
    DateTime? LastPresentedAtUtc,
    DateTime? LastAcceptedAtUtc,
    DateTime? LastDismissedAtUtc,
    bool IsAwaitingResolution);
