using System.Text;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("social-publishing/facebook/webhook")]
[AllowAnonymous]
public sealed class FacebookSocialPublishingWebhookController : ControllerBase
{
    private const long MaximumPayloadSize = 262144;

    private readonly ISocialWebhookHandler webhookHandler;
    private readonly ISocialPublicationService publicationService;

    public FacebookSocialPublishingWebhookController(
        IEnumerable<ISocialWebhookHandler> webhookHandlers,
        ISocialPublicationService publicationService)
    {
        this.webhookHandler = webhookHandlers.Single(handler => handler.Network == SocialNetwork.Facebook);
        this.publicationService = publicationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (!this.webhookHandler.IsEnabled)
        {
            return this.NotFound();
        }

        if (!string.Equals(mode, "subscribe", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(challenge)
            || !this.webhookHandler.VerifySubscriptionToken(verifyToken))
        {
            return this.Forbid();
        }

        return this.Content(challenge, "text/plain", Encoding.UTF8);
    }

    [HttpPost]
    [RequestSizeLimit(MaximumPayloadSize)]
    public async Task<IActionResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (!this.webhookHandler.IsEnabled)
        {
            return this.NotFound();
        }

        using StreamReader reader = new StreamReader(this.Request.Body, Encoding.UTF8, false, leaveOpen: true);
        string payload = await reader.ReadToEndAsync(cancellationToken);
        string? signature = this.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (!this.webhookHandler.VerifySignature(payload, signature))
        {
            return this.Unauthorized();
        }

        IReadOnlyCollection<SocialWebhookChange> changes = this.webhookHandler.ParseChanges(payload);
        foreach (SocialWebhookChange change in changes)
        {
            await this.publicationService.ApplyExternalChangeAsync(
                SocialNetwork.Facebook,
                change,
                cancellationToken);
        }

        return this.Ok();
    }
}
