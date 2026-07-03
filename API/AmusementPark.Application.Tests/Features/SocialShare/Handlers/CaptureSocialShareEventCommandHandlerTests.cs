using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialShare.Commands;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Handlers;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Core.Domain.SocialShare;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialShare.Handlers;

public sealed class CaptureSocialShareEventCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenChannelIsInvalid_ShouldRejectWithoutPersisting()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        CaptureSocialShareEventCommandHandler handler = CreateHandler(repository.Object);

        ApplicationResult<SocialShareEventCaptureResult> result = await handler.HandleAsync(new CaptureSocialShareEventCommand(
            new SocialShareEventCapture("Park", "park-1", "Demo Park", "https://example.test/fr/park/park-1/demo", "fr", "BadChannel", null)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "social-share.event.invalid");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenUrlIsNotHttp_ShouldRejectWithoutPersisting()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        CaptureSocialShareEventCommandHandler handler = CreateHandler(repository.Object);

        ApplicationResult<SocialShareEventCaptureResult> result = await handler.HandleAsync(new CaptureSocialShareEventCommand(
            new SocialShareEventCapture("Park", "park-1", "Demo Park", "javascript:alert(1)", "fr", "Copy", null)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "social-share.event.invalid");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenUrlTargetsAnotherOrigin_ShouldRejectWithoutPersisting()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        CaptureSocialShareEventCommandHandler handler = CreateHandler(repository.Object);

        ApplicationResult<SocialShareEventCaptureResult> result = await handler.HandleAsync(new CaptureSocialShareEventCommand(
            new SocialShareEventCapture("Park", "park-1", "Demo Park", "https://external.test/fr/park/park-1/demo", "fr", "Copy", null)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "social-share.event.invalid");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsPresent_ShouldPersistAuthenticatedEvent()
    {
        DateTime occurredAtUtc = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<SocialShareEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialShareEvent shareEvent, CancellationToken _) =>
            {
                shareEvent.OccurredAtUtc = occurredAtUtc;
                return shareEvent;
            });

        CaptureSocialShareEventCommandHandler handler = CreateHandler(repository.Object);

        ApplicationResult<SocialShareEventCaptureResult> result = await handler.HandleAsync(new CaptureSocialShareEventCommand(
            new SocialShareEventCapture("ParkItem", " item-1 ", "  Demo Attraction  ", "https://example.test/fr/park/p/item/item-1/demo", "fr-FR", "LinkedIn", "user-1")));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Accepted);
        Assert.Equal(occurredAtUtc, result.Value.OccurredAtUtc);
        repository.Verify(repo => repo.CreateAsync(It.Is<SocialShareEvent>(shareEvent =>
            shareEvent.TargetType == SocialShareTargetType.ParkItem &&
            shareEvent.TargetId == "item-1" &&
            shareEvent.TargetTitle == "Demo Attraction" &&
            shareEvent.Url == "https://example.test/fr/park/p/item/item-1/demo" &&
            shareEvent.LanguageCode == "fr" &&
            shareEvent.Channel == SocialShareChannel.LinkedIn &&
            shareEvent.VisitorKind == SocialShareVisitorKind.Authenticated &&
            shareEvent.UserId == "user-1"), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdIsMissing_ShouldPersistAnonymousEvent()
    {
        Mock<ISocialShareEventRepository> repository = new Mock<ISocialShareEventRepository>(MockBehavior.Strict);
        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<SocialShareEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialShareEvent shareEvent, CancellationToken _) => shareEvent);

        CaptureSocialShareEventCommandHandler handler = CreateHandler(repository.Object);

        ApplicationResult<SocialShareEventCaptureResult> result = await handler.HandleAsync(new CaptureSocialShareEventCommand(
            new SocialShareEventCapture("Home", null, null, "https://example.test/fr/home", "fr", "Native", null)));

        Assert.True(result.IsSuccess);
        repository.Verify(repo => repo.CreateAsync(It.Is<SocialShareEvent>(shareEvent =>
            shareEvent.TargetType == SocialShareTargetType.Home &&
            shareEvent.Channel == SocialShareChannel.Native &&
            shareEvent.VisitorKind == SocialShareVisitorKind.Anonymous &&
            shareEvent.UserId == null), It.IsAny<CancellationToken>()), Times.Once);
        repository.VerifyAll();
    }

    private static CaptureSocialShareEventCommandHandler CreateHandler(ISocialShareEventRepository repository)
    {
        return new CaptureSocialShareEventCommandHandler(
            repository,
            new StaticPublicSeoContextProvider("https://example.test"));
    }

    private sealed class StaticPublicSeoContextProvider : IPublicSeoContextProvider
    {
        private readonly string publicBaseUrl;

        public StaticPublicSeoContextProvider(string publicBaseUrl)
        {
            this.publicBaseUrl = publicBaseUrl;
        }

        public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
        {
            PublicSeoContext context = new PublicSeoContext(this.publicBaseUrl, Array.Empty<string>());
            return Task.FromResult(context);
        }
    }
}
