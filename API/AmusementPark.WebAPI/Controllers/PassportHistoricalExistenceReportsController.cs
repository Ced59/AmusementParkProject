using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/visits/{visitId}/historical-existence-reports")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportHistoricalExistenceReportsController : ControllerBase
{
    private readonly IQueryHandler<
        ListOwnedHistoricalExistenceReportsQuery,
        ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>> queryHandler;
    private readonly ICommandHandler<
        SubmitHistoricalExistenceReportCommand,
        ApplicationResult<HistoricalExistenceReportResult>> commandHandler;

    public PassportHistoricalExistenceReportsController(
        IQueryHandler<
            ListOwnedHistoricalExistenceReportsQuery,
            ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>>> queryHandler,
        ICommandHandler<
            SubmitHistoricalExistenceReportCommand,
            ApplicationResult<HistoricalExistenceReportResult>> commandHandler)
    {
        this.queryHandler = queryHandler;
        this.commandHandler = commandHandler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<HistoricalExistenceReportDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
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

        ApplicationResult<IReadOnlyCollection<HistoricalExistenceReportResult>> result =
            await this.queryHandler.HandleAsync(
                new ListOwnedHistoricalExistenceReportsQuery(userId, visitId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static report => report.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalExistenceReports)]
    [ProducesResponseType(typeof(HistoricalExistenceReportDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitAsync(
        [FromRoute] string visitId,
        [FromBody] SubmitHistoricalExistenceReportRequestDto request,
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

        ApplicationResult<HistoricalExistenceReportResult> result =
            await this.commandHandler.HandleAsync(
                request.ToCommand(userId, visitId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.StatusCode(StatusCodes.Status201Created, result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
