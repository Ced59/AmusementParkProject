using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("notification-email/unsubscribe")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class NotificationEmailUnsubscribeController : ControllerBase
{
    private readonly ICommandHandler<UnsubscribeNotificationEmailCommand, ApplicationResult> handler;

    public NotificationEmailUnsubscribeController(
        ICommandHandler<UnsubscribeNotificationEmailCommand, ApplicationResult> handler)
    {
        this.handler = handler;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.NotificationEmailUnsubscribe)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnsubscribeAsync(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.handler.HandleAsync(
            new UnsubscribeNotificationEmailCommand(token),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
