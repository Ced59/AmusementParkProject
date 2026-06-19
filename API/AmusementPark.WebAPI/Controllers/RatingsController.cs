using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("ratings")]
public sealed class RatingsController : ControllerBase
{
    private readonly ICommandHandler<UpsertUserRatingCommand, ApplicationResult<UserRatingResult>> upsertUserRatingCommandHandler;
    private readonly IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler;
    private readonly IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>> getUserRatingQueryHandler;
    private readonly IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>> listUserRatingsQueryHandler;
    private readonly IQueryHandler<GetUserRatingStatsQuery, ApplicationResult<UserRatingStatsResult>> getUserRatingStatsQueryHandler;
    private readonly IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<RatingRankingItemResult>>> getRatingRankingsQueryHandler;

    public RatingsController(
        ICommandHandler<UpsertUserRatingCommand, ApplicationResult<UserRatingResult>> upsertUserRatingCommandHandler,
        IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler,
        IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>> getUserRatingQueryHandler,
        IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>> listUserRatingsQueryHandler,
        IQueryHandler<GetUserRatingStatsQuery, ApplicationResult<UserRatingStatsResult>> getUserRatingStatsQueryHandler,
        IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<RatingRankingItemResult>>> getRatingRankingsQueryHandler)
    {
        this.upsertUserRatingCommandHandler = upsertUserRatingCommandHandler;
        this.getRatingSummaryQueryHandler = getRatingSummaryQueryHandler;
        this.getUserRatingQueryHandler = getUserRatingQueryHandler;
        this.listUserRatingsQueryHandler = listUserRatingsQueryHandler;
        this.getUserRatingStatsQueryHandler = getUserRatingStatsQueryHandler;
        this.getRatingRankingsQueryHandler = getRatingRankingsQueryHandler;
    }

    [HttpGet("{targetType}/{targetId}/summary")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync([FromRoute] string targetType, [FromRoute] string targetId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingSummaryResult> result = await this.getRatingSummaryQueryHandler.HandleAsync(
            new GetRatingSummaryQuery(targetType.ToRatingTargetType(), targetId),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("rankings")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [ProducesResponseType(typeof(PagedResponseDto<RatingRankingItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRankingsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? targetType = null,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        RatingTargetType? parsedTargetType = string.IsNullOrWhiteSpace(targetType) ? null : targetType.ToRatingTargetType();
        ParkItemCategory? parsedCategory = category.ToParkItemCategoryFilter();
        ApplicationResult<PagedResult<RatingRankingItemResult>> result = await this.getRatingRankingsQueryHandler.HandleAsync(
            new GetRatingRankingsQuery(parsedTargetType, parsedCategory, pagination.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()));
    }

    [HttpGet("me")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PagedResponseDto<UserRatingListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRatingsAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<PagedResult<UserRatingListItemResult>> result = await this.listUserRatingsQueryHandler.HandleAsync(
            new ListUserRatingsQuery(userId, pagination.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static rating => rating.ToHttp()));
    }

    [HttpGet("me/stats")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserRatingStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRatingStatsAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<UserRatingStatsResult> result = await this.getUserRatingStatsQueryHandler.HandleAsync(new GetUserRatingStatsQuery(userId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{targetType}/{targetId}/me")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserRatingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRatingForTargetAsync([FromRoute] string targetType, [FromRoute] string targetId, CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<UserRatingResult?> result = await this.getUserRatingQueryHandler.HandleAsync(
            new GetUserRatingQuery(userId, targetType.ToRatingTargetType(), targetId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value?.ToHttp());
    }

    [HttpPut]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(UserRatingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAsync([FromBody] UserRatingUpsertDto request, CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<UserRatingResult> result = await this.upsertUserRatingCommandHandler.HandleAsync(
            new UpsertUserRatingCommand(
                userId,
                request.TargetType.ToRatingTargetType(),
                request.TargetId,
                request.Value),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
