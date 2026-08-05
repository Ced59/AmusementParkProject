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

public sealed class AdminSocialPublicationsControllerTests
{
    [Fact]
    public async Task PublishAsync_ShouldForwardAuthenticatedAdminAndReturnPublication()
    {
        Mock<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>> publishHandler =
            new Mock<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        publishHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<PublishSocialLinkCommand>(command =>
                    command.Request.Network == SocialNetwork.Facebook
                    && command.Request.Message == "Message"
                    && command.Request.Url == "https://amusement-parks.fun/fr/home"
                    && command.RequestedByUserId == "admin-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Published,
                Message = "Message",
                Url = "https://amusement-parks.fun/fr/home",
            }));

        AdminSocialPublicationsController controller = new AdminSocialPublicationsController(
            Mock.Of<IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview>>(MockBehavior.Strict),
            publishHandler.Object,
            Mock.Of<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<UpdateSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult>>(MockBehavior.Strict));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "admin-1") },
                    "test")),
            },
        };

        IActionResult result = await controller.PublishAsync(
            new PublishSocialLinkRequestDto
            {
                Network = SocialNetworkDto.Facebook,
                Message = "Message",
                Url = "https://amusement-parks.fun/fr/home",
            },
            CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        SocialPublicationDto response = Assert.IsType<SocialPublicationDto>(okResult.Value);
        Assert.Equal("publication-1", response.Id);
        Assert.Equal("Published", response.Status);
        publishHandler.VerifyAll();
    }
}
