using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record RecordGlobalRatingSuggestionInteractionCommand(
    string UserId,
    RatingTargetType TargetType,
    string TargetId,
    GlobalRatingSuggestionInteractionType InteractionType,
    DateTime PresentedAtUtc)
    : ICommand<ApplicationResult<GlobalRatingSuggestionPreferenceResult>>;
