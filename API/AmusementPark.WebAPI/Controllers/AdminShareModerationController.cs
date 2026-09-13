using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Sharing;
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
[Route("admin/share-moderation")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminShareModerationController : ControllerBase
{
    private readonly IQueryHandler<GetShareModerationReportsQuery,
        ApplicationResult<PagedResult<ShareModerationReportResult>>> queryHandler;
    private readonly ICommandHandler<ReviewShareModerationReportCommand, ApplicationResult>
        commandHandler;

    public AdminShareModerationController(
        IQueryHandler<GetShareModerationReportsQuery,
            ApplicationResult<PagedResult<ShareModerationReportResult>>> queryHandler,
        ICommandHandler<ReviewShareModerationReportCommand, ApplicationResult> commandHandler)
    {
        this.queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        this.commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    [HttpGet("reports")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PagedResponseDto<ShareModerationReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] ShareModerationReportSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCriteria(out ShareModerationReportSearchCriteria? criteria)
            || criteria is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<ShareModerationReportResult>> result =
            await this.queryHandler.HandleAsync(
                new GetShareModerationReportsQuery(criteria),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static report => report.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpPut("reports/{reportId}")]
    [EnableRateLimiting(RateLimitPolicyNames.ShareModerationAdministration)]
    [AdminAudit("share-moderation.report.review", "ShareModerationReport", TargetIdRouteKey = "reportId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReviewAsync(
        [FromRoute] string reportId,
        [FromBody] ReviewShareModerationReportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? reviewerUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(reviewerUserId)
            || !request.TryToCommand(
                reviewerUserId,
                reportId,
                out ReviewShareModerationReportCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.commandHandler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
