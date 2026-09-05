using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record GetUserRatingQuery(
    string UserId,
    RatingTargetType TargetType,
    string TargetId) : IQuery<ApplicationResult<UserRatingResult?>>;
