using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record GetUserParkRatingRankingsQuery(
    string UserId,
    PagedQuery Paging,
    string? ParkSearch = null,
    bool PublicTargetsOnly = false,
    string? TargetId = null) : IQuery<ApplicationResult<PagedResult<UserParkRatingRankingResult>>>;
