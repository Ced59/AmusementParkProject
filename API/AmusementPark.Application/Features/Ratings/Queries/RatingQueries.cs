using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record GetRatingSummaryQuery(
    RatingTargetType TargetType,
    string TargetId) : IQuery<ApplicationResult<RatingSummaryResult>>;

public sealed record GetUserRatingQuery(
    string UserId,
    RatingTargetType TargetType,
    string TargetId) : IQuery<ApplicationResult<UserRatingResult?>>;

public sealed record ListUserRatingsQuery(
    string UserId,
    PagedQuery Paging) : IQuery<ApplicationResult<PagedResult<UserRatingListItemResult>>>;

public sealed record GetUserRatingStatsQuery(
    string UserId) : IQuery<ApplicationResult<UserRatingStatsResult>>;

public sealed record GetRatingRankingsQuery(
    RatingTargetType? TargetType,
    ParkItemCategory? ParkItemCategory,
    PagedQuery Paging) : IQuery<ApplicationResult<PagedResult<RatingRankingItemResult>>>;
