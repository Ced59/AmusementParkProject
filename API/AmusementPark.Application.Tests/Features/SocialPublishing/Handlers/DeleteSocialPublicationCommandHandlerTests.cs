using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Handlers;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Handlers;

public sealed class DeleteSocialPublicationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeleteManualFacebookPostRequestedBySameUser()
    {
        SocialPublication publication = new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Trigger = SocialPublicationTrigger.Manual,
            RequestedByUserId = "editor-1",
            CreatedByUserId = "editor-1",
        };
        Mock<ISocialPublicationRepository> repository = new Mock<ISocialPublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync("publication-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publication);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service.Setup(value => value.DeleteAsync("publication-1", "editor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));

        DeleteSocialPublicationCommandHandler handler = new DeleteSocialPublicationCommandHandler(service.Object, repository.Object);
        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new DeleteSocialPublicationCommand("publication-1", "editor-1", RequireRequesterOwnership: true));

        Assert.True(result.IsSuccess);
        service.VerifyAll();
    }

    [Theory]
    [InlineData("another-editor", SocialPublicationTrigger.Manual)]
    [InlineData("editor-1", SocialPublicationTrigger.AutomaticParkPublication)]
    public async Task HandleAsync_ShouldRejectOtherUsersAndAutomaticPosts(
        string requester,
        SocialPublicationTrigger trigger)
    {
        SocialPublication publication = new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Trigger = trigger,
            RequestedByUserId = "editor-1",
            CreatedByUserId = "editor-1",
        };
        Mock<ISocialPublicationRepository> repository = new Mock<ISocialPublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync("publication-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publication);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);

        DeleteSocialPublicationCommandHandler handler = new DeleteSocialPublicationCommandHandler(service.Object, repository.Object);
        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new DeleteSocialPublicationCommand("publication-1", requester, RequireRequesterOwnership: true));

        Assert.False(result.IsSuccess);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseImmutableCreatorAfterAnotherUserUpdatesPost()
    {
        SocialPublication publication = new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Trigger = SocialPublicationTrigger.Manual,
            RequestedByUserId = "admin-1",
            CreatedByUserId = "editor-1",
            LastSynchronizedAtUtc = DateTime.UtcNow,
        };
        Mock<ISocialPublicationRepository> repository = new Mock<ISocialPublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync("publication-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publication);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service.Setup(value => value.DeleteAsync("publication-1", "editor-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));

        DeleteSocialPublicationCommandHandler handler = new DeleteSocialPublicationCommandHandler(service.Object, repository.Object);
        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new DeleteSocialPublicationCommand("publication-1", "editor-1", RequireRequesterOwnership: true));

        Assert.True(result.IsSuccess);
        service.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ShouldAllowOnlyUnmodifiedLegacyInitialPublication(bool modified)
    {
        DateTime requestedAtUtc = DateTime.UtcNow.AddHours(-1);
        SocialPublication publication = new SocialPublication
        {
            Id = "publication-1",
            Network = SocialNetwork.Facebook,
            Trigger = SocialPublicationTrigger.Manual,
            RequestedByUserId = "editor-1",
            RequestedAtUtc = requestedAtUtc,
            AttemptedAtUtc = requestedAtUtc.AddSeconds(2),
            LastSynchronizedAtUtc = modified ? requestedAtUtc.AddMinutes(2) : null,
        };
        Mock<ISocialPublicationRepository> repository = new Mock<ISocialPublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync("publication-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publication);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        if (!modified)
        {
            service.Setup(value => value.DeleteAsync("publication-1", "editor-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));
        }

        DeleteSocialPublicationCommandHandler handler = new DeleteSocialPublicationCommandHandler(service.Object, repository.Object);
        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new DeleteSocialPublicationCommand("publication-1", "editor-1", RequireRequesterOwnership: true));

        Assert.Equal(!modified, result.IsSuccess);
        service.VerifyAll();
    }
}
