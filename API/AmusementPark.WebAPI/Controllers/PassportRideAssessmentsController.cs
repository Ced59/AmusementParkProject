using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
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
[Route("me/passport/occurrences/{occurrenceId}/assessment")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportRideAssessmentsController : ControllerBase
{
    private readonly ICommandHandler<
        UpsertRideAssessmentCommand,
        ApplicationResult<RideOccurrenceResult>> upsertHandler;
    private readonly ICommandHandler<
        DeleteRideAssessmentCommand,
        ApplicationResult<RideOccurrenceResult>> deleteHandler;

    public PassportRideAssessmentsController(
        ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>> upsertHandler,
        ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>> deleteHandler)
    {
        this.upsertHandler = upsertHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpPut]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertAsync(
        [FromRoute] string occurrenceId,
        [FromBody] UpsertPassportRideAssessmentRequestDto request,
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

        ApplicationResult<RideOccurrenceResult> result = await this.upsertHandler.HandleAsync(
            new UpsertRideAssessmentCommand(
                userId,
                occurrenceId,
                request.Value,
                request.PrivateComment,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(PassportRideOccurrenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string occurrenceId,
        [FromQuery] long expectedVersion,
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

        ApplicationResult<RideOccurrenceResult> result = await this.deleteHandler.HandleAsync(
            new DeleteRideAssessmentCommand(userId, occurrenceId, expectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
