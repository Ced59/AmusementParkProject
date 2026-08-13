using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Handlers;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Handlers;

public sealed class RetryParkAnnouncementPublicationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldForwardParkPublicationAndAuthenticatedEditor()
    {
        SocialPublication publication = CreatePublication();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service
            .Setup(candidate => candidate.RetryParkAnnouncementAsync(
                "park-1",
                "publication-1",
                "editor-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));
        RetryParkAnnouncementPublicationCommandHandler handler =
            new RetryParkAnnouncementPublicationCommandHandler(service.Object);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryParkAnnouncementPublicationCommand("park-1", "publication-1", "editor-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(publication, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenServiceRejectsPublication_ShouldForwardFailure()
    {
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service
            .Setup(candidate => candidate.RetryParkAnnouncementAsync(
                "park-2",
                "publication-1",
                "editor-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Failure(
                AmusementPark.Application.Features.SocialPublishing.SocialPublishingApplicationErrors.PublicationNotFound("publication-1")));
        RetryParkAnnouncementPublicationCommandHandler handler =
            new RetryParkAnnouncementPublicationCommandHandler(service.Object);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryParkAnnouncementPublicationCommand("park-2", "publication-1", "editor-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        service.VerifyAll();
    }

    private static SocialPublication CreatePublication()
    {
        return new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Status = SocialPublicationStatus.Failed,
            Trigger = SocialPublicationTrigger.AutomaticParkPublication,
            Message = "Announcement",
            Url = "https://amusement-parks.fun/fr/park/park-1/test",
            SourceEntityType = "Park",
            SourceEntityId = "park-1",
            DeduplicationKey = "facebook:park:park-1",
        };
    }
}
