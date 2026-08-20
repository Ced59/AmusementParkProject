using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
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

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("ratings")]
public sealed class UserRankingSharesController : ControllerBase
{
    private readonly IQueryHandler<GetUserRankingShareSettingsQuery, ApplicationResult<UserRankingShareSettingsResult>> getSettingsHandler;
    private readonly ICommandHandler<SetUserRankingShareVisibilityCommand, ApplicationResult<UserRankingShareSettingsResult>> setVisibilityHandler;
    private readonly IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>> getProfileHandler;
    private readonly IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> getParkRankingsHandler;
    private readonly IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> getParkItemRankingsHandler;
    private readonly IQueryHandler<GetSharedUserRankingPreviewQuery, ApplicationResult<UserRankingSharePreviewFileResult>> getPreviewHandler;

    public UserRankingSharesController(
        IQueryHandler<GetUserRankingShareSettingsQuery, ApplicationResult<UserRankingShareSettingsResult>> getSettingsHandler,
        ICommandHandler<SetUserRankingShareVisibilityCommand, ApplicationResult<UserRankingShareSettingsResult>> setVisibilityHandler,
        IQueryHandler<GetSharedUserRankingProfileQuery, ApplicationResult<SharedUserRankingProfileResult>> getProfileHandler,
        IQueryHandler<GetSharedUserParkRatingRankingsQuery, ApplicationResult<PagedResult<UserParkRatingRankingResult>>> getParkRankingsHandler,
        IQueryHandler<GetSharedUserParkItemRatingRankingsQuery, ApplicationResult<PagedResult<UserParkItemRatingRankingResult>>> getParkItemRankingsHandler,
        IQueryHandler<GetSharedUserRankingPreviewQuery, ApplicationResult<UserRankingSharePreviewFileResult>> getPreviewHandler)
    {
        this.getSettingsHandler = getSettingsHandler;
        this.setVisibilityHandler = setVisibilityHandler;
        this.getProfileHandler = getProfileHandler;
        this.getParkRankingsHandler = getParkRankingsHandler;
        this.getParkItemRankingsHandler = getParkItemRankingsHandler;
        this.getPreviewHandler = getPreviewHandler;
    }

    [HttpGet("me/share")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(UserRankingShareSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyShareSettingsAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<UserRankingShareSettingsResult> result = await this.getSettingsHandler.HandleAsync(
            new GetUserRankingShareSettingsQuery(userId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("me/share")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(UserRankingShareSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMyShareVisibilityAsync(
        [FromBody] UserRankingShareVisibilityDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<UserRankingShareSettingsResult> result = await this.setVisibilityHandler.HandleAsync(
            new SetUserRankingShareVisibilityCommand(userId, request.IsPublic),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("shared/{shareId}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharedUserRankingProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedProfileAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SharedUserRankingProfileResult> result = await this.getProfileHandler.HandleAsync(
            new GetSharedUserRankingProfileQuery(shareId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp(this.User.GetUserId()))
            : this.ToActionResult(result);
    }

    [HttpGet("shared/{shareId}/parks")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PagedResponseDto<UserParkRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSharedParkRankingsAsync(
        [FromRoute] string shareId,
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<UserParkRatingRankingResult>> result = await this.getParkRankingsHandler.HandleAsync(
            new GetSharedUserParkRatingRankingsQuery(shareId, pagination.ToApplication(), search),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpGet("shared/{shareId}/park-items")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PagedResponseDto<UserParkItemRatingRankingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSharedParkItemRankingsAsync(
        [FromRoute] string shareId,
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

        ApplicationResult<PagedResult<UserParkItemRatingRankingResult>> result = await this.getParkItemRankingsHandler.HandleAsync(
            new GetSharedUserParkItemRatingRankingsQuery(
                shareId,
                parsedCategory.Value,
                pagination.ToApplication(),
                search,
                type.ToParkItemTypeFilter()),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpGet("shared/{shareId}/preview.png")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedPreviewAsync(
        [FromRoute] string shareId,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        ParkItemCategory? parsedCategory = string.IsNullOrWhiteSpace(category)
            ? null
            : category.ToParkItemCategoryFilter();
        if (!string.IsNullOrWhiteSpace(category) && !parsedCategory.HasValue)
        {
            return this.BadRequest();
        }

        ApplicationResult<UserRankingSharePreviewFileResult> result = await this.getPreviewHandler.HandleAsync(
            new GetSharedUserRankingPreviewQuery(
                shareId,
                parsedCategory,
                type.ToParkItemTypeFilter()),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.File(result.Value.Content, result.Value.ContentType)
            : this.ToActionResult(result);
    }
}
