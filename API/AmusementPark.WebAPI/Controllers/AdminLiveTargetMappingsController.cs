using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/live/mappings")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminLiveTargetMappingsController : ControllerBase
{
    private readonly IQueryHandler<
        GetAdminLiveTargetMappingsQuery,
        ApplicationResult<PagedResult<LiveTargetMappingResult>>> queryHandler;
    private readonly ICommandHandler<
        CreateLiveTargetMappingCandidateCommand,
        ApplicationResult<LiveTargetMappingResult>> createHandler;
    private readonly ICommandHandler<
        ReviewLiveTargetMappingCommand,
        ApplicationResult<LiveTargetMappingResult>> reviewHandler;

    public AdminLiveTargetMappingsController(
        IQueryHandler<
            GetAdminLiveTargetMappingsQuery,
            ApplicationResult<PagedResult<LiveTargetMappingResult>>> queryHandler,
        ICommandHandler<
            CreateLiveTargetMappingCandidateCommand,
            ApplicationResult<LiveTargetMappingResult>> createHandler,
        ICommandHandler<
            ReviewLiveTargetMappingCommand,
            ApplicationResult<LiveTargetMappingResult>> reviewHandler)
    {
        this.queryHandler = queryHandler;
        this.createHandler = createHandler;
        this.reviewHandler = reviewHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<LiveTargetMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sourceId = null,
        [FromQuery] LiveMappingStatusDto? status = null,
        [FromQuery] LiveTargetTypeDto? targetType = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        LiveTargetMappingSearchCriteria criteria = new LiveTargetMappingSearchCriteria(
            page,
            pageSize,
            sourceId,
            status.HasValue ? (LiveMappingStatus)status.Value : null,
            targetType.HasValue ? (LiveTargetType)targetType.Value : null,
            search);
        ApplicationResult<PagedResult<LiveTargetMappingResult>> result =
            await this.queryHandler.HandleAsync(
                new GetAdminLiveTargetMappingsQuery(criteria),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static mapping => mapping.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpPost("candidates")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [AdminAudit("live.mapping.create", "LiveTargetMapping", StaticTargetId = "new")]
    [ProducesResponseType(typeof(LiveTargetMappingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCandidateAsync(
        [FromBody] CreateLiveTargetMappingCandidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<LiveTargetMappingResult> result = await this.createHandler.HandleAsync(
            request.ToCommand(),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{mappingId:guid}/review")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [AdminAudit(
        "live.mapping.review",
        "LiveTargetMapping",
        TargetIdRouteKey = "mappingId")]
    [ProducesResponseType(typeof(LiveTargetMappingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewAsync(
        [FromRoute] Guid mappingId,
        [FromBody] ReviewLiveTargetMappingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? reviewerUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(reviewerUserId))
        {
            return this.BadRequest();
        }

        ApplicationResult<LiveTargetMappingResult> result = await this.reviewHandler.HandleAsync(
            request.ToCommand(mappingId, reviewerUserId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
