using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.SocialPublishing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkDataEditorParksControllerTests
{
    [Fact]
    public async Task GetDataCompletenessAsync_ShouldForwardPublicationProjection()
    {
        Mock<IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>> handler =
            new Mock<IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<GetParkDataCompletenessScoreQuery>(query =>
                    query.ParkId == "park-1"
                    && query.IncludeHidden
                    && query.ProjectForPublication),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<DataCompletenessScore>.Success(DataCompletenessScore.FromPoints(
                100,
                100,
                "public-text.forbidden-editorial-language")));

        ParkDataEditorParksController controller = new ParkDataEditorParksController(
            Mock.Of<IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            handler.Object,
            Mock.Of<ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>>(MockBehavior.Strict));

        IActionResult result = await controller.GetDataCompletenessAsync("park-1", true, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DataCompletenessScoreDto score = Assert.IsType<DataCompletenessScoreDto>(ok.Value);
        Assert.Equal(95, score.CompletenessScore);
        Assert.Equal(new[] { "public-text.forbidden-editorial-language" }, score.PublicationBlockers);
        handler.VerifyAll();
    }

    [Fact]
    public async Task RefreshSocialPreviewAsync_ShouldForwardParkAndAuthenticatedEditor()
    {
        Mock<ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>> handler =
            new Mock<ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<RefreshParkAnnouncementPreviewCommand>(command =>
                    command.ParkId == "park-1"
                    && command.RequestedByUserId == "editor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Published,
                Trigger = SocialPublicationTrigger.AutomaticParkPublication,
                Message = "Announcement",
                Url = "https://amusement-parks.fun/fr/park/park-1/test",
            }));

        ParkDataEditorParksController controller = new ParkDataEditorParksController(
            Mock.Of<IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>>(MockBehavior.Strict),
            handler.Object,
            Mock.Of<ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>>(MockBehavior.Strict));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "editor-1") },
                    "test")),
            },
        };

        IActionResult result = await controller.RefreshSocialPreviewAsync("park-1", CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SocialPublicationDto response = Assert.IsType<SocialPublicationDto>(ok.Value);
        Assert.Equal("publication-1", response.Id);
        handler.VerifyAll();
    }

    [Fact]
    public async Task ListPublishedSocialPreviewsAsync_ShouldReturnOnlyParkIdentifiers()
    {
        Mock<IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>> handler =
            new Mock<IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.IsAny<ListPublishedParkAnnouncementIdsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park-1", "park-2" });

        ParkDataEditorParksController controller = new ParkDataEditorParksController(
            Mock.Of<IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            handler.Object);

        IActionResult result = await controller.ListPublishedSocialPreviewsAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyCollection<ParkSocialPreviewPublicationDto> response =
            Assert.IsAssignableFrom<IReadOnlyCollection<ParkSocialPreviewPublicationDto>>(ok.Value);
        Assert.Collection(
            response,
            first => Assert.Equal("park-1", first.ParkId),
            second => Assert.Equal("park-2", second.ParkId));
        handler.VerifyAll();
    }

    [Fact]
    public async Task RetrySocialPreviewPublicationAsync_ShouldForwardParkPublicationAndAuthenticatedEditor()
    {
        Mock<ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>> handler =
            new Mock<ICommandHandler<RetryParkAnnouncementPublicationCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict);
        handler
            .Setup(candidate => candidate.HandleAsync(
                It.Is<RetryParkAnnouncementPublicationCommand>(command =>
                    command.ParkId == "park-1"
                    && command.PublicationId == "publication-1"
                    && command.RequestedByUserId == "editor-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(new SocialPublication
            {
                Id = "publication-1",
                Network = SocialNetwork.Facebook,
                Status = SocialPublicationStatus.Published,
                Trigger = SocialPublicationTrigger.AutomaticParkPublication,
                Message = "Announcement",
                Url = "https://amusement-parks.fun/fr/park/park-1/test",
                SourceEntityType = "Park",
                SourceEntityId = "park-1",
            }));

        ParkDataEditorParksController controller = new ParkDataEditorParksController(
            Mock.Of<IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>>(MockBehavior.Strict),
            Mock.Of<IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>>>(MockBehavior.Strict),
            Mock.Of<ICommandHandler<RefreshParkAnnouncementPreviewCommand, ApplicationResult<SocialPublication>>>(MockBehavior.Strict),
            handler.Object,
            Mock.Of<IQueryHandler<ListPublishedParkAnnouncementIdsQuery, IReadOnlyCollection<string>>>(MockBehavior.Strict));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "editor-1") },
                    "test")),
            },
        };

        IActionResult result = await controller.RetrySocialPreviewPublicationAsync(
            "park-1",
            "publication-1",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SocialPublicationDto response = Assert.IsType<SocialPublicationDto>(ok.Value);
        Assert.Equal("publication-1", response.Id);
        handler.VerifyAll();
    }
}
