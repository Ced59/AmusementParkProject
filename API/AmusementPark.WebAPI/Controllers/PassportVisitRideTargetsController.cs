using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
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
[Route("me/passport/visits/{visitId}/ride-targets")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportVisitRideTargetsController : ControllerBase
{
    private readonly IQueryHandler<
        EvaluateVisitRideTargetsQuery,
        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>> evaluateHandler;

    public PassportVisitRideTargetsController(
        IQueryHandler<
            EvaluateVisitRideTargetsQuery,
            ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>>> evaluateHandler)
    {
        this.evaluateHandler = evaluateHandler;
    }

    [HttpPost(":evaluate")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PassportVisitRideTargetEvaluationDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateAsync(
        [FromRoute] string visitId,
        [FromBody] EvaluatePassportVisitRideTargetsRequestDto request,
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

        ApplicationResult<IReadOnlyCollection<VisitRideTargetEvaluationResult>> result =
            await this.evaluateHandler.HandleAsync(
                new EvaluateVisitRideTargetsQuery(userId, visitId, request.ParkItemIds),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static evaluation => evaluation.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }
}
