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

public sealed record GetRatingDiagnosticsQuery : IQuery<ApplicationResult<RatingDiagnosticsResult>>;

public sealed record GetCurrentRatingMethodologyQuery : IQuery<ApplicationResult<RatingMethodologyResult>>;

public sealed record GetRatingMethodologyQuery(
    string Version) : IQuery<ApplicationResult<RatingMethodologyResult>>;

public sealed record ListRatingMethodologiesQuery
    : IQuery<ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>>;

public sealed record GetUserRatingQuery(
    string UserId,
    RatingTargetType TargetType,
    string TargetId) : IQuery<ApplicationResult<UserRatingResult?>>;

public sealed record ListUserRatingsQuery(
    string UserId,
    PagedQuery Paging,
    string? ParkSearch = null) : IQuery<ApplicationResult<PagedResult<UserRatingListItemResult>>>;

public sealed record GetUserRatingStatsQuery(
    string UserId) : IQuery<ApplicationResult<UserRatingStatsResult>>;

public sealed record GetRatingRankingsQuery(
    ParkItemCategory? ParkItemCategory,
    PagedQuery Paging,
    string? ParkSearch = null) : IQuery<ApplicationResult<PagedResult<ParkRatingRankingResult>>>;

public sealed record GetParkItemRatingRankingsQuery(
    ParkItemCategory ParkItemCategory,
    PagedQuery Paging,
    string? Search = null,
    ParkItemType? ParkItemType = null) : IQuery<ApplicationResult<PagedResult<ParkItemRatingRankingResult>>>;

public sealed record GetUserParkRatingRankingsQuery(
    string UserId,
    PagedQuery Paging,
    string? ParkSearch = null,
    bool PublicTargetsOnly = false) : IQuery<ApplicationResult<PagedResult<UserParkRatingRankingResult>>>;

public sealed record GetUserParkItemRatingRankingsQuery(
    string UserId,
    ParkItemCategory ParkItemCategory,
    PagedQuery Paging,
    string? Search = null,
    ParkItemType? ParkItemType = null,
    bool PublicTargetsOnly = false) : IQuery<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>;

public sealed record GetUserRankingShareSettingsQuery(
    string UserId) : IQuery<ApplicationResult<UserRankingShareSettingsResult>>;

public sealed record GetSharedUserRankingProfileQuery(
    string ShareId) : IQuery<ApplicationResult<SharedUserRankingProfileResult>>;

public sealed record GetSharedUserParkRatingRankingsQuery(
    string ShareId,
    PagedQuery Paging,
    string? ParkSearch = null) : IQuery<ApplicationResult<PagedResult<UserParkRatingRankingResult>>>;

public sealed record GetSharedUserParkItemRatingRankingsQuery(
    string ShareId,
    ParkItemCategory ParkItemCategory,
    PagedQuery Paging,
    string? Search = null,
    ParkItemType? ParkItemType = null) : IQuery<ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>>;

public sealed record GetSharedUserRankingPreviewQuery(
    string ShareId,
    ParkItemCategory? ParkItemCategory = null,
    ParkItemType? ParkItemType = null) : IQuery<ApplicationResult<UserRankingSharePreviewFileResult>>;
