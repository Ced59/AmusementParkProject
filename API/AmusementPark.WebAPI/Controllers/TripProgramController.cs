using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
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
[Route("me/trips/{tripPlanId}")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripProgramController : ControllerBase
{
    private readonly IQueryHandler<GetTripProgramQuery, ApplicationResult<TripProgramResult>> getHandler;
    private readonly ICommandHandler<AddTripParkCandidateCommand,
        ApplicationResult<CreateTripParkCandidateResult>> addCandidateHandler;
    private readonly ICommandHandler<UpdateTripParkCandidateCommand,
        ApplicationResult<TripParkCandidateResult>> updateCandidateHandler;
    private readonly ICommandHandler<ChangeTripParkCandidateStateCommand,
        ApplicationResult<TripParkCandidateResult>> stateHandler;
    private readonly ICommandHandler<MoveTripParkCandidateCommand,
        ApplicationResult<TripProgramResult>> moveHandler;
    private readonly ICommandHandler<DeleteTripParkCandidateCommand, ApplicationResult> deleteCandidateHandler;
    private readonly ICommandHandler<PutTripDayPlanCommand,
        ApplicationResult<TripDayPlanResult>> putDayHandler;
    private readonly ICommandHandler<DeleteTripDayPlanCommand, ApplicationResult> deleteDayHandler;

    public TripProgramController(
        IQueryHandler<GetTripProgramQuery, ApplicationResult<TripProgramResult>> getHandler,
        ICommandHandler<AddTripParkCandidateCommand,
            ApplicationResult<CreateTripParkCandidateResult>> addCandidateHandler,
        ICommandHandler<UpdateTripParkCandidateCommand,
            ApplicationResult<TripParkCandidateResult>> updateCandidateHandler,
        ICommandHandler<ChangeTripParkCandidateStateCommand,
            ApplicationResult<TripParkCandidateResult>> stateHandler,
        ICommandHandler<MoveTripParkCandidateCommand,
            ApplicationResult<TripProgramResult>> moveHandler,
        ICommandHandler<DeleteTripParkCandidateCommand, ApplicationResult> deleteCandidateHandler,
        ICommandHandler<PutTripDayPlanCommand,
            ApplicationResult<TripDayPlanResult>> putDayHandler,
        ICommandHandler<DeleteTripDayPlanCommand, ApplicationResult> deleteDayHandler)
    {
        this.getHandler = getHandler;
        this.addCandidateHandler = addCandidateHandler;
        this.updateCandidateHandler = updateCandidateHandler;
        this.stateHandler = stateHandler;
        this.moveHandler = moveHandler;
        this.deleteCandidateHandler = deleteCandidateHandler;
        this.putDayHandler = putDayHandler;
        this.deleteDayHandler = deleteDayHandler;
    }

    [HttpGet("program")]
    [ProducesResponseType(typeof(TripProgramDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripProgramResult> result = await this.getHandler.HandleAsync(
            new GetTripProgramQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("parks")]
    [ProducesResponseType(typeof(TripParkCandidateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TripParkCandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddCandidateAsync(
        [FromRoute] string tripPlanId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] AddTripParkCandidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || !request.TryToApplication(out TripParkCandidateInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<CreateTripParkCandidateResult> result = await this.addCandidateHandler.HandleAsync(
            new AddTripParkCandidateCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                idempotencyKey,
                input),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        TripParkCandidateDto response = result.Value.Candidate.ToHttp();
        return result.Value.WasReplayed
            ? this.Ok(response)
            : this.StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("parks/{candidateId}")]
    [ProducesResponseType(typeof(TripParkCandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCandidateAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string candidateId,
        [FromBody] UpdateTripParkCandidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out TripParkCandidateDetailsInput? input) || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripParkCandidateResult> result = await this.updateCandidateHandler.HandleAsync(
            new UpdateTripParkCandidateCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                candidateId,
                request.ExpectedCandidateVersion,
                input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("parks/{candidateId}/state")]
    [ProducesResponseType(typeof(TripParkCandidateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeCandidateStateAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string candidateId,
        [FromBody] ChangeTripParkCandidateStateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!TripProgramHttpMapper.TryParseEnum(request.State, out TripParkCandidateState state))
        {
            return this.BadRequest();
        }

        ApplicationResult<TripParkCandidateResult> result = await this.stateHandler.HandleAsync(
            new ChangeTripParkCandidateStateCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                candidateId,
                request.ExpectedCandidateVersion,
                state),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("parks/{candidateId}/move")]
    [ProducesResponseType(typeof(TripProgramDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MoveCandidateAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string candidateId,
        [FromBody] MoveTripParkCandidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!TripProgramHttpMapper.TryParseEnum(request.Placement, out TripParkCandidatePlacement placement))
        {
            return this.BadRequest();
        }

        ApplicationResult<TripProgramResult> result = await this.moveHandler.HandleAsync(
            new MoveTripParkCandidateCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                candidateId,
                request.AnchorCandidateId,
                placement),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("parks/{candidateId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCandidateAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string candidateId,
        [FromQuery] long expectedPlanVersion,
        [FromQuery] long expectedCandidateVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteCandidateHandler.HandleAsync(
            new DeleteTripParkCandidateCommand(
                userId,
                tripPlanId,
                expectedPlanVersion,
                candidateId,
                expectedCandidateVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPut("days/{localDate}")]
    [ProducesResponseType(typeof(TripDayPlanDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PutDayAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string localDate,
        [FromBody] PutTripDayPlanRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!TripProgramHttpMapper.TryParseDate(localDate, out DateOnly parsedDate)
            || !request.TryToApplication(out TripDayPlanInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripDayPlanResult> result = await this.putDayHandler.HandleAsync(
            new PutTripDayPlanCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                parsedDate,
                request.ExpectedDayVersion,
                input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("days/{localDate}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDayAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string localDate,
        [FromQuery] long expectedPlanVersion,
        [FromQuery] long expectedDayVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!TripProgramHttpMapper.TryParseDate(localDate, out DateOnly parsedDate))
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.deleteDayHandler.HandleAsync(
            new DeleteTripDayPlanCommand(
                userId,
                tripPlanId,
                expectedPlanVersion,
                parsedDate,
                expectedDayVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
