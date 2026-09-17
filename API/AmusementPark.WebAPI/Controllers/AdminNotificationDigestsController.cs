using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/notification-digests")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminNotificationDigestsController : ControllerBase
{
    private readonly IQueryHandler<PreviewNotificationDigestQuery, NotificationDigestPreviewResult?>
        previewHandler;

    public AdminNotificationDigestsController(
        IQueryHandler<PreviewNotificationDigestQuery, NotificationDigestPreviewResult?> previewHandler)
    {
        this.previewHandler = previewHandler ?? throw new ArgumentNullException(nameof(previewHandler));
    }

    [HttpPost("preview")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(NotificationDigestPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewAsync(
        [FromBody] NotificationDigestPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(
                request.Frequency,
                ignoreCase: true,
                out NotificationFrequency frequency)
            || frequency is not NotificationFrequency.DailyDigest
                and not NotificationFrequency.WeeklyDigest
            || request.PeriodStartUtc.Kind != DateTimeKind.Utc)
        {
            return this.BadRequest();
        }

        NotificationDigestPreviewResult? result;
        try
        {
            NotificationDigestPeriodResolver.ValidateGroup(
                NotificationChannel.Email,
                frequency,
                request.PeriodStartUtc);
            result = await this.previewHandler.HandleAsync(
                new PreviewNotificationDigestQuery(
                    request.UserId,
                    frequency,
                    request.PeriodStartUtc),
                cancellationToken);
        }
        catch (ArgumentException)
        {
            return this.BadRequest();
        }

        return result is null ? this.NotFound() : this.Ok(result.ToHttp());
    }
}
