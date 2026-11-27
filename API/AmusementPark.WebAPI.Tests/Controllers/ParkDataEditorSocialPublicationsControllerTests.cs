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
            publishHandler.Object,
            Mock.Of<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict))
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

    [Fact]
    public async Task DeleteOwnFacebookAsync_ShouldRequireRequesterOwnership()
    {
        Mock<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>> deleteHandler =
            new Mock<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        deleteHandler.Setup(handler => handler.HandleAsync(
                It.Is<DeleteSocialPublicationCommand>(command =>
                    command.PublicationId == "publication-1"
                    && command.RequestedByUserId == "editor-1"
                    && command.RequireRequesterOwnership),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Deleted,
                Message = "Deleted",
                Url = "https://amusement-parks.fun/fr/home",
            }));

        ParkDataEditorSocialPublicationsController controller = new ParkDataEditorSocialPublicationsController(
            Mock.Of<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            deleteHandler.Object)
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

        IActionResult result = await controller.DeleteOwnFacebookAsync("publication-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        deleteHandler.VerifyAll();
    }

    [Fact]
    public async Task RetryOwnFacebookAsync_ShouldRequireRequesterOwnership()
    {
        Mock<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>> retryHandler =
            new Mock<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        retryHandler.Setup(handler => handler.HandleAsync(
                It.Is<RetrySocialPublicationCommand>(command =>
                    command.PublicationId == "publication-1"
                    && command.RequestedByUserId == "editor-1"
                    && command.RequireRequesterOwnership),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Published,
                Message = "Published",
                Url = "https://amusement-parks.fun/fr/home",
            }));
        ParkDataEditorSocialPublicationsController controller = new ParkDataEditorSocialPublicationsController(
            Mock.Of<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            retryHandler.Object,
            Mock.Of<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict))
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

        IActionResult result = await controller.RetryOwnFacebookAsync("publication-1", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        retryHandler.VerifyAll();
    }
}
