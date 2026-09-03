using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
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
[Route("me/passport/visits")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportVisitsController : ControllerBase
{
    private readonly ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>> createHandler;
    private readonly IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>> listHandler;
    private readonly IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>> getHandler;

    public PassportVisitsController(
        ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>> createHandler,
        IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>> listHandler,
        IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>> getHandler)
    {
        this.createHandler = createHandler;
        this.listHandler = listHandler;
        this.getHandler = getHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PassportVisitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreatePassportVisitRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        ApplicationResult<CreateVisitResult> result = await this.createHandler.HandleAsync(
            request.ToApplication(userId, idempotencyKey ?? string.Empty),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        if (result.Value.WasReplayed)
        {
            this.Response.Headers["Idempotency-Replayed"] = "true";
        }

        PassportVisitDto response = result.Value.Visit.ToHttp();
        return this.CreatedAtAction(
            nameof(GetByIdAsync),
            new { visitId = response.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PassportVisitPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] PassportVisitListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        if (!PassportVisitCursorCodec.TryDecode(request.Cursor, out UserVisitListCursor? cursor))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "Le curseur de pagination est invalide.",
                "visit.cursor.invalid");
        }

        ApplicationResult<VisitPageResult> result = await this.listHandler.HandleAsync(
            request.ToApplication(userId, cursor),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("{visitId}")]
    [ProducesResponseType(typeof(PassportVisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] string visitId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        ApplicationResult<VisitResult> result = await this.getHandler.HandleAsync(
            new GetVisitQuery(userId, visitId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
