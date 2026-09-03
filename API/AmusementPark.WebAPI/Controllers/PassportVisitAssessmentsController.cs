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
[Route("me/passport/visits/{visitId}/assessment")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportVisitAssessmentsController : ControllerBase
{
    private readonly ICommandHandler<
        UpsertVisitParkAssessmentCommand,
        ApplicationResult<VisitResult>> upsertHandler;
    private readonly ICommandHandler<
        DeleteVisitParkAssessmentCommand,
        ApplicationResult<VisitResult>> deleteHandler;

    public PassportVisitAssessmentsController(
        ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>> upsertHandler,
        ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>> deleteHandler)
    {
        this.upsertHandler = upsertHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpPut]
    [ProducesResponseType(typeof(PassportVisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertAsync(
        [FromRoute] string visitId,
        [FromBody] UpsertPassportVisitParkAssessmentRequestDto request,
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

        ApplicationResult<VisitResult> result = await this.upsertHandler.HandleAsync(
            new UpsertVisitParkAssessmentCommand(
                userId,
                visitId,
                request.Value,
                request.PrivateComment,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(PassportVisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string visitId,
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

        ApplicationResult<VisitResult> result = await this.deleteHandler.HandleAsync(
            new DeleteVisitParkAssessmentCommand(userId, visitId, expectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
