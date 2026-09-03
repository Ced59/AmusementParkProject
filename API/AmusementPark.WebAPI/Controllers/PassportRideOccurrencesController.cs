using System.ComponentModel.DataAnnotations;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/visits/{visitId}")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportRideOccurrencesController : ControllerBase
{
    private readonly ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>> addHandler;
    private readonly ICommandHandler<UpdateRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>> updateHandler;
    private readonly ICommandHandler<DeleteRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>> deleteHandler;
    private readonly ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>> reorderHandler;
    private readonly IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>> getHandler;
    private readonly IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>> listHandler;

    public PassportRideOccurrencesController(
        ICommandHandler<AddRideOccurrencesBatchCommand, ApplicationResult<CreateRideOccurrencesResult>> addHandler,
        ICommandHandler<UpdateRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>> updateHandler,
        ICommandHandler<DeleteRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>> deleteHandler,
        ICommandHandler<ReorderRideOccurrenceCommand, ApplicationResult<ReorderRideOccurrenceResult>> reorderHandler,
        IQueryHandler<GetRideOccurrenceQuery, ApplicationResult<RideOccurrenceResult>> getHandler,
        IQueryHandler<ListRideOccurrencesQuery, ApplicationResult<RideOccurrencePageResult>> listHandler)
    {
        this.addHandler = addHandler;
        this.updateHandler = updateHandler;
        this.deleteHandler = deleteHandler;
        this.reorderHandler = reorderHandler;
        this.getHandler = getHandler;
        this.listHandler = listHandler;
    }

    [HttpPost("occurrences")]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddAsync(
        [FromRoute] string visitId,
        [FromBody] CreatePassportRideOccurrenceRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<CreateRideOccurrencesResult> result = await this.addHandler.HandleAsync(
            request.ToApplication(userId, visitId, idempotencyKey),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        SetReplayHeader(this.Response, result.Value.WasReplayed);
        SetOrderNormalizedHeader(this.Response, result.Value.WasNormalized);
        RideOccurrenceResult occurrence = result.Value.Occurrences.Single();
        PassportRideOccurrenceDto response = occurrence.ToHttp();
        return this.Created(
            BuildOccurrenceLocation(this.Request, visitId, response.Id),
            response);
    }

    [HttpPost("occurrences:batch")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PassportRideOccurrenceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddBatchAsync(
        [FromRoute] string visitId,
        [FromBody] CreatePassportRideOccurrencesBatchRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<CreateRideOccurrencesResult> result = await this.addHandler.HandleAsync(
            request.ToApplication(userId, visitId, idempotencyKey),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        SetReplayHeader(this.Response, result.Value.WasReplayed);
        SetOrderNormalizedHeader(this.Response, result.Value.WasNormalized);
        return this.StatusCode(
            StatusCodes.Status201Created,
            result.Value.Occurrences.Select(static occurrence => occurrence.ToHttp()).ToArray());
    }

    [HttpGet("occurrences/{occurrenceId}")]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string visitId,
        [FromRoute] string occurrenceId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<RideOccurrenceResult> result = await this.getHandler.HandleAsync(
            new GetRideOccurrenceQuery(userId, visitId, occurrenceId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("occurrences")]
    [ProducesResponseType(typeof(PassportRideOccurrencePageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] string visitId,
        [FromQuery] PassportRideOccurrenceListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        if (!PassportRideOccurrenceCursorCodec.TryDecode(
            request.Cursor,
            out RideOccurrenceListCursor? cursor))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "Le curseur de pagination est invalide.",
                "ride-occurrence.cursor.invalid");
        }

        ApplicationResult<RideOccurrencePageResult> result = await this.listHandler.HandleAsync(
            request.ToApplication(userId, visitId, cursor),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPatch("occurrences/{occurrenceId}")]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string visitId,
        [FromRoute] string occurrenceId,
        [FromBody] UpdatePassportRideOccurrenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<RideOccurrenceResult> result = await this.updateHandler.HandleAsync(
            request.ToApplication(userId, visitId, occurrenceId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("occurrences/{occurrenceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string visitId,
        [FromRoute] string occurrenceId,
        [FromQuery, Range(1, long.MaxValue)] long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<RideOccurrenceResult> result = await this.deleteHandler.HandleAsync(
            new DeleteRideOccurrenceCommand(userId, visitId, occurrenceId, expectedVersion),
            cancellationToken);
        return result.IsSuccess
            ? this.NoContent()
            : this.ToActionResult(result);
    }

    [HttpPost("occurrences:reorder")]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderAsync(
        [FromRoute] string visitId,
        [FromBody] ReorderPassportRideOccurrenceRequestDto request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResult();
        }

        ApplicationResult<ReorderRideOccurrenceResult> result =
            await this.reorderHandler.HandleAsync(
                new ReorderRideOccurrenceCommand(
                    userId,
                    visitId,
                    idempotencyKey,
                    request.OccurrenceId,
                    request.ExpectedVersion,
                    request.AnchorOccurrenceId,
                    (RideOccurrencePlacement)request.Placement),
                cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        SetReplayHeader(this.Response, result.Value.WasReplayed);
        SetOrderNormalizedHeader(this.Response, result.Value.WasNormalized);

        return this.Ok(result.Value.Occurrence.ToHttp());
    }

    internal static string BuildOccurrenceLocation(
        HttpRequest request,
        string visitId,
        string occurrenceId)
    {
        return $"{request.GetPublicPathPrefix()}/me/passport/visits/{Uri.EscapeDataString(visitId)}/occurrences/{Uri.EscapeDataString(occurrenceId)}";
    }

    private IActionResult UnauthorizedResult()
    {
        return this.ToProblemDetailsResult(
            StatusCodes.Status401Unauthorized,
            "Authentication is required.",
            "auth.unauthorized");
    }

    private static void SetReplayHeader(HttpResponse response, bool wasReplayed)
    {
        if (wasReplayed)
        {
            response.Headers["Idempotency-Replayed"] = "true";
        }
    }

    private static void SetOrderNormalizedHeader(HttpResponse response, bool wasNormalized)
    {
        if (wasNormalized)
        {
            response.Headers["Ride-Order-Normalized"] = "true";
        }
    }
}
