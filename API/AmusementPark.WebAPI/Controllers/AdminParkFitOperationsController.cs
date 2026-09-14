using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Commands;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkFit;
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
[Route("admin/park-fit")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminParkFitOperationsController : ControllerBase
{
    private readonly IQueryHandler<
        GetParkFitSourceReportsQuery,
        ApplicationResult<PagedResult<ParkFitSourceReportResult>>> reportQueryHandler;
    private readonly ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult>
        reportCommandHandler;
    private readonly ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult>
        statusCommandHandler;

    public AdminParkFitOperationsController(
        IQueryHandler<
            GetParkFitSourceReportsQuery,
            ApplicationResult<PagedResult<ParkFitSourceReportResult>>> reportQueryHandler,
        ICommandHandler<ReviewParkFitSourceReportCommand, ApplicationResult> reportCommandHandler,
        ICommandHandler<ChangeParkFitOperationalStatusCommand, ApplicationResult> statusCommandHandler)
    {
        this.reportQueryHandler = reportQueryHandler;
        this.reportCommandHandler = reportCommandHandler;
        this.statusCommandHandler = statusCommandHandler;
    }

    [HttpGet("reports")]
    [ProducesResponseType(typeof(PagedResponseDto<ParkFitSourceReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsAsync(
        [FromQuery] ParkFitSourceReportSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCriteria(out ParkFitSourceReportSearchCriteria? criteria)
            || criteria is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<ParkFitSourceReportResult>> result =
            await this.reportQueryHandler.HandleAsync(
                new GetParkFitSourceReportsQuery(criteria),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static report => report.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpPut("reports/{reportId}")]
    [EnableRateLimiting(RateLimitPolicyNames.ParkFitAdministration)]
    [AdminAudit("park-fit.report.review", "ParkFitSourceReport", TargetIdRouteKey = "reportId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReviewReportAsync(
        [FromRoute] string reportId,
        [FromBody] ReviewParkFitSourceReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? reviewerUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(reviewerUserId)
            || !request.TryToCommand(
                reportId,
                reviewerUserId,
                out ReviewParkFitSourceReportCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.reportCommandHandler.HandleAsync(
            command,
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPut("parks/{parkId}/operational-status")]
    [EnableRateLimiting(RateLimitPolicyNames.ParkFitAdministration)]
    [AdminAudit("park-fit.operational-status.change", "Park", TargetIdRouteKey = "parkId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeOperationalStatusAsync(
        [FromRoute] string parkId,
        [FromBody] ChangeParkFitOperationalStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId)
            || !request.TryToCommand(
                parkId,
                actorUserId,
                out ChangeParkFitOperationalStatusCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.statusCommandHandler.HandleAsync(
            command,
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
