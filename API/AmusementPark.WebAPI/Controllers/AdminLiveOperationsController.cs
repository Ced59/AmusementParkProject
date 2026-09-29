using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/live/operations")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminLiveOperationsController : ControllerBase
{
    private readonly IQueryHandler<
        GetAdminLiveOperationsQuery,
        ApplicationResult<LiveOperationsDashboardResult>> queryHandler;
    private readonly ICommandHandler<
        UpdateLiveOperationalControlCommand,
        ApplicationResult<LiveOperationalScopeResult>> updateHandler;

    public AdminLiveOperationsController(
        IQueryHandler<
            GetAdminLiveOperationsQuery,
            ApplicationResult<LiveOperationsDashboardResult>> queryHandler,
        ICommandHandler<
            UpdateLiveOperationalControlCommand,
            ApplicationResult<LiveOperationalScopeResult>> updateHandler)
    {
        this.queryHandler = queryHandler;
        this.updateHandler = updateHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LiveOperationsDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<LiveOperationsDashboardResult> result =
            await this.queryHandler.HandleAsync(
                new GetAdminLiveOperationsQuery(),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("controls")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [AdminAudit("live.operations.control.update", "LiveOperationalControl", StaticTargetId = "scope")]
    [InvalidatesPublicCache(PublicCacheScope.LiveData)]
    [ProducesResponseType(typeof(LiveOperationalScopeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateControlAsync(
        [FromBody] UpdateLiveOperationalControlRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? changedByUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(changedByUserId))
        {
            return this.BadRequest();
        }

        ApplicationResult<LiveOperationalScopeResult> result =
            await this.updateHandler.HandleAsync(
                request.ToCommand(changedByUserId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
