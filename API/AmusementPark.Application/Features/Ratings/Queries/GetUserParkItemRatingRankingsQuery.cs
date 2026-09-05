using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record GetUserParkItemRatingRankingsQuery(
    string UserId,
    ParkItemCategory ParkItemCategory,
    PagedQuery Paging,
    string? Search = null,
    ParkItemType? ParkItemType = null,
    bool PublicTargetsOnly = false,
    string? TargetId = null) : IQuery<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>;
