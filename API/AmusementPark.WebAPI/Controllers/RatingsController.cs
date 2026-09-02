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
    private readonly ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>> deleteUserRatingCommandHandler;
    private readonly IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler;
    private readonly IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>> getUserRatingQueryHandler;
    private readonly IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>> listUserRatingsQueryHandler;
    private readonly IQueryHandler<GetUserRatingStatsQuery, ApplicationResult<UserRatingStatsResult>> getUserRatingStatsQueryHandler;
    private readonly IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<ParkRatingRankingResult>>> getRatingRankingsQueryHandler;
    private readonly IQueryHandler<GetParkItemRatingRankingsQuery, ApplicationResult<PagedResult<ParkItemRatingRankingResult>>> getParkItemRatingRankingsQueryHandler;
    private readonly IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> getUserParkRatingRankingsQueryHandler;
    private readonly IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> getUserParkItemRatingRankingsQueryHandler;

    public RatingsController(
        ICommandHandler<UpsertUserRatingCommand, ApplicationResult<UserRatingResult>> upsertUserRatingCommandHandler,
        ICommandHandler<DeleteUserRatingCommand, ApplicationResult<RatingSummaryResult>> deleteUserRatingCommandHandler,
        IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler,
        IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>> getUserRatingQueryHandler,
        IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>> listUserRatingsQueryHandler,
        IQueryHandler<GetUserRatingStatsQuery, ApplicationResult<UserRatingStatsResult>> getUserRatingStatsQueryHandler,
        IQueryHandler<GetRatingRankingsQuery, ApplicationResult<PagedResult<ParkRatingRankingResult>>> getRatingRankingsQueryHandler,
        IQueryHandler<GetParkItemRatingRankingsQuery, ApplicationResult<PagedResult<ParkItemRatingRankingResult>>> getParkItemRatingRankingsQueryHandler,
        IQueryHandler<GetUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> getUserParkRatingRankingsQueryHandler,
        IQueryHandler<GetUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> getUserParkItemRatingRankingsQueryHandler)
    {
        this.upsertUserRatingCommandHandler = upsertUserRatingCommandHandler;
        this.deleteUserRatingCommandHandler = deleteUserRatingCommandHandler;
        this.getRatingSummaryQueryHandler = getRatingSummaryQueryHandler;
        this.getUserRatingQueryHandler = getUserRatingQueryHandler;
        this.listUserRatingsQueryHandler = listUserRatingsQueryHandler;
        this.getUserRatingStatsQueryHandler = getUserRatingStatsQueryHandler;
        this.getRatingRankingsQueryHandler = getRatingRankingsQueryHandler;
        this.getParkItemRatingRankingsQueryHandler = getParkItemRatingRankingsQueryHandler;
        this.getUserParkRatingRankingsQueryHandler = getUserParkRatingRankingsQueryHandler;
        this.getUserParkItemRatingRankingsQueryHandler = getUserParkItemRatingRankingsQueryHandler;
    }

    [HttpGet("{targetType}/{targetId}/summary")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicRatingDataShort)]
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
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicRatingDataShort)]
    [ProducesResponseType(typeof(PagedResponseDto<ParkRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRankingsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ParkItemCategory? parsedCategory = category.ToParkItemCategoryFilter();
        ApplicationResult<PagedResult<ParkRatingRankingResult>> result = await this.getRatingRankingsQueryHandler.HandleAsync(
            new GetRatingRankingsQuery(parsedCategory, pagination.ToApplication(), search),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()));
    }

    [HttpGet("rankings/park-items")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicRatingDataShort)]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkItemRankingsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ParkItemCategory? parsedCategory = category.ToParkItemCategoryFilter();
        if (!parsedCategory.HasValue)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<ParkItemRatingRankingResult>> result =
            await this.getParkItemRatingRankingsQueryHandler.HandleAsync(
                new GetParkItemRatingRankingsQuery(
                    parsedCategory.Value,
                    pagination.ToApplication(),
                    search,
                    type.ToParkItemTypeFilter()),
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
    public async Task<IActionResult> GetMyRatingsAsync([FromQuery] PaginationRequestDto pagination, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<PagedResult<UserRatingListItemResult>> result = await this.listUserRatingsQueryHandler.HandleAsync(
            new ListUserRatingsQuery(userId, pagination.ToApplication(), search),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static rating => rating.ToHttp()));
    }

    [HttpGet("me/rankings/parks")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PagedResponseDto<UserParkRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyParkRankingsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<PagedResult<UserParkRatingRankingResult>> result =
            await this.getUserParkRatingRankingsQueryHandler.HandleAsync(
                new GetUserParkRatingRankingsQuery(userId, pagination.ToApplication(), search),
                cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static ranking => ranking.ToHttp()));
    }

    [HttpGet("me/rankings/park-items")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PagedResponseDto<UserParkItemRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyParkItemRankingsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ParkItemCategory? parsedCategory = category.ToParkItemCategoryFilter();
        if (!parsedCategory.HasValue)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result =
            await this.getUserParkItemRatingRankingsQueryHandler.HandleAsync(
                new GetUserParkItemRatingRankingsQuery(
                    userId,
                    parsedCategory.Value,
                    pagination.ToApplication(),
                    search,
                    type.ToParkItemTypeFilter()),
                cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static ranking => ranking.ToHttp()));
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

    [HttpDelete("{targetType}/{targetId}/me")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMyRatingForTargetAsync(
        [FromRoute] string targetType,
        [FromRoute] string targetId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<RatingSummaryResult> result = await this.deleteUserRatingCommandHandler.HandleAsync(
            new DeleteUserRatingCommand(userId, targetType.ToRatingTargetType(), targetId),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
