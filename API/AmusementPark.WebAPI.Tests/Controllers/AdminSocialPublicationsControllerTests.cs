using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
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
                    && command.Request.PreviewImageId == "image-1"
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
            Mock.Of<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>>(MockBehavior.Strict),
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
                PreviewImageId = "image-1",
            },
            CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        SocialPublicationDto response = Assert.IsType<SocialPublicationDto>(okResult.Value);
        Assert.Equal("publication-1", response.Id);
        Assert.Equal("Published", response.Status);
        publishHandler.VerifyAll();
    }

    [Fact]
    public async Task GetDraftAsync_ShouldForwardUrlAndImagePagination()
    {
        Mock<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>> draftHandler =
            new Mock<IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>>(MockBehavior.Strict);
        SocialPublicationDraft draft = new SocialPublicationDraft(
            "https://amusement-parks.fun/fr/home",
            "Message",
            SocialPublicationTargetKind.Page,
            "Accueil",
            null,
            null,
            new PagedResult<SocialPublicationImageOption>(Array.Empty<SocialPublicationImageOption>(), 2, 6, 0));
        draftHandler.Setup(handler => handler.HandleAsync(
                It.Is<GetSocialPublicationDraftQuery>(query =>
                    query.Url == "https://amusement-parks.fun/fr/home"
                    && query.ImagePage == 2
                    && query.ImagePageSize == 6),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublicationDraft>.Success(draft));
        AdminSocialPublicationsController controller = new AdminSocialPublicationsController(
            Mock.Of<IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview>>(MockBehavior.Strict),
            draftHandler.Object,
            Mock.Of<ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<UpdateSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult>>(MockBehavior.Strict));

        IActionResult result = await controller.GetDraftAsync(
            "https://amusement-parks.fun/fr/home",
            new AmusementPark.WebAPI.Contracts.Common.PaginationRequestDto { Page = 2, Size = 6 },
            CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        SocialPublicationDraftDto response = Assert.IsType<SocialPublicationDraftDto>(okResult.Value);
        Assert.Equal("Accueil", response.TargetName);
        Assert.Equal(2, response.Images.Pagination?.CurrentPage);
        draftHandler.VerifyAll();
    }
}
