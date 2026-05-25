using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Queries;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.LocalizedContent;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Administration des champs localisés par JSON contrôlé.
/// </summary>
[ApiController]
[Route("localized-content")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class LocalizedContentController : ControllerBase
{
    private readonly IQueryHandler<SearchLocalizedContentTargetsQuery, ApplicationResult<PagedResult<LocalizedContentTargetResult>>> searchTargetsQueryHandler;
    private readonly ICommandHandler<ApplyLocalizedContentJsonCommand, ApplicationResult<LocalizedContentApplyResult>> applyJsonCommandHandler;

    public LocalizedContentController(
        IQueryHandler<SearchLocalizedContentTargetsQuery, ApplicationResult<PagedResult<LocalizedContentTargetResult>>> searchTargetsQueryHandler,
        ICommandHandler<ApplyLocalizedContentJsonCommand, ApplicationResult<LocalizedContentApplyResult>> applyJsonCommandHandler)
    {
        this.searchTargetsQueryHandler = searchTargetsQueryHandler;
        this.applyJsonCommandHandler = applyJsonCommandHandler;
    }

    [HttpGet("targets")]
    [ProducesResponseType(typeof(PagedResponseDto<LocalizedContentTargetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchTargetsAsync(
        [FromQuery] string entityType,
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<LocalizedContentTargetResult>> result = await this.searchTargetsQueryHandler.HandleAsync(
            new SearchLocalizedContentTargetsQuery(entityType, search, pagination.Page, pagination.Size),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<LocalizedContentTargetDto> response = result.Value.ToPagedResponse(static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpPatch("{entityType}/{entityId}")]
    [AdminAudit("localized-content.json.apply", "LocalizedContent", TargetIdRouteKey = "entityId")]
    [ProducesResponseType(typeof(LocalizedContentApplyResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyJsonAsync(
        [FromRoute] string entityType,
        [FromRoute] string entityId,
        [FromBody] ApplyLocalizedContentJsonRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<LocalizedContentApplyResult> result = await this.applyJsonCommandHandler.HandleAsync(
            new ApplyLocalizedContentJsonCommand(entityType, entityId, request.Json.GetRawText()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
