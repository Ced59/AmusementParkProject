using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Contracts.SocialPublishing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorSocialPublicationsControllerTests
{
    [Fact]
    public async Task PublishFacebookAsync_ShouldForceFacebookAndForwardOptionalFields()
    {
        Mock<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>> publishHandler =
            new Mock<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        publishHandler.Setup(handler => handler.HandleAsync(
                It.Is<PublishSocialLinkCommand>(command =>
                    command.Request.Network == SocialNetwork.Facebook
                    && command.Request.Message == null
                    && command.Request.PreviewImageId == "image-1"
                    && command.RequestedByUserId == "editor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Published,
                Message = "Default",
                Url = "https://amusement-parks.fun/fr/home",
            }));
        ParkDataEditorSocialPublicationsController controller = new ParkDataEditorSocialPublicationsController(
            Mock.Of<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>>(MockBehavior.Strict),
            publishHandler.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "editor-1") },
                        "test")),
                },
            },
        };

        IActionResult result = await controller.PublishFacebookAsync(
            new PublishSocialLinkRequestDto
            {
                Network = (SocialNetworkDto)999,
                Url = "https://amusement-parks.fun/fr/home",
                PreviewImageId = "image-1",
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        publishHandler.VerifyAll();
    }
}
