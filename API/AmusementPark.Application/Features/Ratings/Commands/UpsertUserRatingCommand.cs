using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Commands;

public sealed record UpsertUserRatingCommand(
    string UserId,
    RatingTargetType TargetType,
    string TargetId,
    double Value) : ICommand<ApplicationResult<UserRatingResult>>;
