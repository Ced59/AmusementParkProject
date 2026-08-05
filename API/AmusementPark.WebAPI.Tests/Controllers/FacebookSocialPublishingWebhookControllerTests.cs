using System.Text;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class FacebookSocialPublishingWebhookControllerTests
{
    [Fact]
    public async Task ReceiveAsync_WhenSignatureIsValid_ShouldApplyParsedChanges()
    {
        const string payload = "{\"object\":\"page\"}";
        SocialWebhookChange change = new SocialWebhookChange("123_456", SocialWebhookChangeKind.Deleted, null);
        Mock<ISocialWebhookHandler> webhookHandler = new Mock<ISocialWebhookHandler>(MockBehavior.Strict);
        webhookHandler.SetupGet(handler => handler.Network).Returns(SocialNetwork.Facebook);
        webhookHandler.SetupGet(handler => handler.IsEnabled).Returns(true);
        webhookHandler.Setup(handler => handler.VerifySignature(payload, "sha256=valid")).Returns(true);
        webhookHandler.Setup(handler => handler.ParseChanges(payload)).Returns(new[] { change });
        Mock<ISocialPublicationService> publicationService = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        publicationService
            .Setup(service => service.ApplyExternalChangeAsync(
                SocialNetwork.Facebook,
                change,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        FacebookSocialPublishingWebhookController controller = new FacebookSocialPublishingWebhookController(
            new[] { webhookHandler.Object },
            publicationService.Object);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        httpContext.Request.Headers["X-Hub-Signature-256"] = "sha256=valid";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        IActionResult result = await controller.ReceiveAsync(CancellationToken.None);

        Assert.IsType<OkResult>(result);
        webhookHandler.VerifyAll();
        publicationService.VerifyAll();
    }

    [Fact]
    public void Verify_WhenTokenIsValid_ShouldReturnMetaChallenge()
    {
        Mock<ISocialWebhookHandler> webhookHandler = new Mock<ISocialWebhookHandler>(MockBehavior.Strict);
        webhookHandler.SetupGet(handler => handler.Network).Returns(SocialNetwork.Facebook);
        webhookHandler.SetupGet(handler => handler.IsEnabled).Returns(true);
        webhookHandler.Setup(handler => handler.VerifySubscriptionToken("verify-token")).Returns(true);
        FacebookSocialPublishingWebhookController controller = new FacebookSocialPublishingWebhookController(
            new[] { webhookHandler.Object },
            Mock.Of<ISocialPublicationService>());

        IActionResult result = controller.Verify("subscribe", "verify-token", "challenge-value");

        ContentResult content = Assert.IsType<ContentResult>(result);
        Assert.Equal("challenge-value", content.Content);
        webhookHandler.VerifyAll();
    }
}
