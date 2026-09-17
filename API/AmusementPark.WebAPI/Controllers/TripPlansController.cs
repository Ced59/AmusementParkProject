using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/trips")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripPlansController : ControllerBase
{
    private readonly IQueryHandler<ListMyTripPlansQuery,
        ApplicationResult<IReadOnlyCollection<TripPlanResult>>> listHandler;
    private readonly IQueryHandler<GetMyTripPlanQuery, ApplicationResult<TripPlanResult>> getHandler;
    private readonly ICommandHandler<CreateTripPlanCommand,
        ApplicationResult<CreateTripPlanResult>> createHandler;
    private readonly ICommandHandler<RenameTripPlanCommand,
        ApplicationResult<TripPlanResult>> renameHandler;
    private readonly ICommandHandler<SetTripPlanDatesCommand,
        ApplicationResult<TripPlanResult>> setDatesHandler;
    private readonly ICommandHandler<DeleteTripPlanCommand, ApplicationResult> deleteHandler;

    public TripPlansController(
        IQueryHandler<ListMyTripPlansQuery,
            ApplicationResult<IReadOnlyCollection<TripPlanResult>>> listHandler,
        IQueryHandler<GetMyTripPlanQuery, ApplicationResult<TripPlanResult>> getHandler,
        ICommandHandler<CreateTripPlanCommand,
            ApplicationResult<CreateTripPlanResult>> createHandler,
        ICommandHandler<RenameTripPlanCommand,
            ApplicationResult<TripPlanResult>> renameHandler,
        ICommandHandler<SetTripPlanDatesCommand,
            ApplicationResult<TripPlanResult>> setDatesHandler,
        ICommandHandler<DeleteTripPlanCommand, ApplicationResult> deleteHandler)
    {
        this.listHandler = listHandler;
        this.getHandler = getHandler;
        this.createHandler = createHandler;
        this.renameHandler = renameHandler;
        this.setDatesHandler = setDatesHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TripPlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<IReadOnlyCollection<TripPlanResult>> result = await this.listHandler.HandleAsync(
            new ListMyTripPlansQuery(userId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static trip => trip.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpGet("{tripPlanId}")]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripPlanResult> result = await this.getHandler.HandleAsync(
            new GetMyTripPlanQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] TripPlanWriteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || !request.TryToApplication(out TripPlanDetailsInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<CreateTripPlanResult> result = await this.createHandler.HandleAsync(
            new CreateTripPlanCommand(userId, idempotencyKey, input),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        TripPlanDto response = result.Value.TripPlan.ToHttp();
        if (result.Value.WasReplayed)
        {
            this.Response.Headers["Idempotency-Replayed"] = "true";
            return this.Ok(response);
        }

        return this.CreatedAtAction(
            nameof(this.GetAsync),
            new { tripPlanId = response.TripPlanId },
            response);
    }

    [HttpPost("{tripPlanId}/rename")]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RenameAsync(
        [FromRoute] string tripPlanId,
        [FromBody] RenameTripPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripPlanResult> result = await this.renameHandler.HandleAsync(
            new RenameTripPlanCommand(userId, tripPlanId, request.ExpectedVersion, request.Title),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{tripPlanId}/dates")]
    [ProducesResponseType(typeof(TripPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetDatesAsync(
        [FromRoute] string tripPlanId,
        [FromBody] SetTripPlanDatesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out TripPlanDatesInput? input) || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripPlanResult> result = await this.setDatesHandler.HandleAsync(
            new SetTripPlanDatesCommand(userId, tripPlanId, request.ExpectedVersion, input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("{tripPlanId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string tripPlanId,
        [FromQuery] long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteHandler.HandleAsync(
            new DeleteTripPlanCommand(userId, tripPlanId, expectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
