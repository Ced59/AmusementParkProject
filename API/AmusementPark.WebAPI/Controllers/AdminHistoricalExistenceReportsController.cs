using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.HistoricalExistenceReports.Commands;
using AmusementPark.Application.Features.HistoricalExistenceReports.Models;
using AmusementPark.Application.Features.HistoricalExistenceReports.Queries;
using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
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
[Route("admin/history/existence-reports")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHistoricalExistenceReportsController : ControllerBase
{
    private readonly IQueryHandler<
        GetHistoricalExistenceReportsQuery,
        ApplicationResult<PagedResult<HistoricalExistenceReportResult>>> queryHandler;
    private readonly ICommandHandler<ReviewHistoricalExistenceReportCommand, ApplicationResult>
        commandHandler;

    public AdminHistoricalExistenceReportsController(
        IQueryHandler<
            GetHistoricalExistenceReportsQuery,
            ApplicationResult<PagedResult<HistoricalExistenceReportResult>>> queryHandler,
        ICommandHandler<ReviewHistoricalExistenceReportCommand, ApplicationResult> commandHandler)
    {
        this.queryHandler = queryHandler;
        this.commandHandler = commandHandler;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponseDto<HistoricalExistenceReportDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] HistoricalExistenceReportSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCriteria(out HistoricalExistenceReportSearchCriteria? criteria)
            || criteria is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<HistoricalExistenceReportResult>> result =
            await this.queryHandler.HandleAsync(
                new GetHistoricalExistenceReportsQuery(criteria),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static report => report.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpPut("{reportId}")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalExistenceReportAdministration)]
    [AdminAudit(
        "history.existence-report.review",
        "HistoricalExistenceReport",
        TargetIdRouteKey = "reportId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReviewAsync(
        [FromRoute] string reportId,
        [FromBody] ReviewHistoricalExistenceReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? reviewerUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(reviewerUserId)
            || !request.TryToCommand(
                reportId,
                reviewerUserId,
                out ReviewHistoricalExistenceReportCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.commandHandler.HandleAsync(
            command,
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
