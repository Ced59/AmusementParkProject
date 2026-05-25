using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Commands;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkGraphUpserts;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/park-graph-upserts")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class ParkGraphUpsertsController : ControllerBase
{
    private readonly ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler;
    private readonly ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler;

    public ParkGraphUpsertsController(
        ICommandHandler<PreviewParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> previewHandler,
        ICommandHandler<ApplyParkGraphUpsertCommand, ApplicationResult<ParkGraphUpsertResult>> applyHandler)
    {
        this.previewHandler = previewHandler;
        this.applyHandler = applyHandler;
    }

    [HttpPost("preview")]
    [AdminAudit("park-graph-upsert.preview", "Park")]
    [ProducesResponseType(typeof(ParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewAsync([FromBody] ParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphUpsertResult> result = await this.previewHandler.HandleAsync(
            new PreviewParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("apply")]
    [AdminAudit("park-graph-upsert.apply", "Park")]
    [ProducesResponseType(typeof(ParkGraphUpsertResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyAsync([FromBody] ParkGraphUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkGraphUpsertResult> result = await this.applyHandler.HandleAsync(
            new ApplyParkGraphUpsertCommand(request.ToApplication(), this.GetCurrentUserId()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    private string? GetCurrentUserId()
    {
        return this.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? this.User.FindFirstValue("sub")
            ?? this.User.FindFirstValue("id");
    }
}
