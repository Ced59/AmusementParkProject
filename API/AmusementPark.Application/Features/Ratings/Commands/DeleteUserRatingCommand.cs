using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Commands;

public sealed record DeleteUserRatingCommand(
    string UserId,
    RatingTargetType TargetType,
    string TargetId) : ICommand<ApplicationResult<RatingSummaryResult>>;
