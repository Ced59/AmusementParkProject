using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
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
[Route("admin/notification-email-deliveries")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminNotificationEmailDeliveriesController : ControllerBase
{
    private readonly IQueryHandler<GetNotificationDeliveryMetricsQuery,
        NotificationDeliveryMetricsResult> handler;

    public AdminNotificationEmailDeliveriesController(
        IQueryHandler<GetNotificationDeliveryMetricsQuery,
            NotificationDeliveryMetricsResult> handler)
    {
        this.handler = handler;
    }

    [HttpGet("metrics")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [ProducesResponseType(typeof(NotificationDeliveryMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMetricsAsync(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        DateTime normalizedToUtc = toUtc ?? DateTime.UtcNow;
        DateTime normalizedFromUtc = fromUtc ?? normalizedToUtc.AddDays(-7);
        try
        {
            NotificationDeliveryMetricsResult result = await this.handler.HandleAsync(
                new GetNotificationDeliveryMetricsQuery(normalizedFromUtc, normalizedToUtc),
                cancellationToken);
            return this.Ok(result.ToHttp());
        }
        catch (ArgumentException)
        {
            return this.BadRequest();
        }
    }
}
