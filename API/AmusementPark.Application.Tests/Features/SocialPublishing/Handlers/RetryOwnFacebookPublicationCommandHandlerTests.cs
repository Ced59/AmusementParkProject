using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Handlers;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Tests.Features.SocialPublishing.Services;
using AmusementPark.Core.Domain.SocialPublishing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Handlers;

public sealed class RetryOwnFacebookPublicationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExactPostAlreadyExists_ShouldRecoverWithoutSendingAgain()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.RequestedByUserId = "admin-2";
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        publisher.ReconciliationResult = new SocialPublisherLinkReconciliationResult(
            true, true, false, "123_456", "https://www.facebook.com/123/posts/456", null, null);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(SocialPublicationStatus.Published, publication.Status);
        Assert.Equal("123_456", publication.ExternalPostId);
        Assert.Equal(1, publisher.ReconcileCallCount);
        Assert.Equal(0, publisher.PublishCallCount);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNoMatchingPostExists_ShouldRetryTheSameRecord()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service.Setup(value => value.RetryAsync("publication-1", "editor-1", It.IsAny<CancellationToken>(), publication.UpdatedAtUtc))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, publisher.ReconcileCallCount);
        service.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPreviewImageQueryIsPresent_ShouldReconcileCanonicalMessageUrl()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.Url += "?facebook-image=95f47e399b224fb289d37bc871175cbf";
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        publisher.ReconciliationResult = new SocialPublisherLinkReconciliationResult(
            true, true, false, "123_456", "https://www.facebook.com/123/posts/456", null, null);
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(SocialPublicationStatus.Published, publication.Status);
        Assert.Equal(1, publisher.ReconcileCallCount);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPreviewQueryHasUnexpectedParameter_ShouldStopBeforeReconciliation()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.Url += "?facebook-image=95f47e399b224fb289d37bc871175cbf&other=1";
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, publisher.ReconcileCallCount);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenAnotherUserCalls_ShouldRejectBeforeReconciliation()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        repository.Publications.Add(FailedPublication());
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "other-editor"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, publisher.ReconcileCallCount);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenMessageDoesNotEndWithTargetUrl_ShouldStopWithoutRetry()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.Message = "Same message for multiple target links";
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, publisher.ReconcileCallCount);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("preview-page-not-ready")]
    [InlineData("preview-page-unavailable")]
    public async Task HandleAsync_WhenPreviewFailedBeforeFacebookRequest_ShouldRetryCustomMessageWithoutReconciliation(
        string failureCode)
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.Message = "Custom bilingual announcement without a trailing link";
        publication.FailureCode = failureCode;
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        service.Setup(value => value.RetryAsync("publication-1", "editor-1", It.IsAny<CancellationToken>(), publication.UpdatedAtUtc))
            .ReturnsAsync(ApplicationResult<SocialPublication>.Success(publication));
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, publisher.ReconcileCallCount);
        service.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPreviewFailedButUrlHasUnexpectedQuery_ShouldStopWithoutRetry()
    {
        InMemorySocialPublicationRepository repository = new InMemorySocialPublicationRepository();
        SocialPublication publication = FailedPublication();
        publication.FailureCode = "preview-page-not-ready";
        publication.Url += "?facebook-image=95f47e399b224fb289d37bc871175cbf&other=1";
        repository.Publications.Add(publication);
        StubSocialPublisher publisher = StubSocialPublisher.Configured();
        Mock<ISocialPublicationService> service = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        RetryOwnFacebookPublicationCommandHandler handler = CreateHandler(repository, service.Object, publisher);

        ApplicationResult<SocialPublication> result = await handler.HandleAsync(
            new RetryOwnFacebookPublicationCommand("publication-1", "editor-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal(0, publisher.ReconcileCallCount);
        service.VerifyNoOtherCalls();
    }

    private static SocialPublication FailedPublication() => new SocialPublication
    {
        Id = "publication-1",
        Network = SocialNetwork.Facebook,
        Status = SocialPublicationStatus.Failed,
        Trigger = SocialPublicationTrigger.Manual,
        RequestedByUserId = "editor-1",
        CreatedByUserId = "editor-1",
        Message = "Salta cable car https://amusement-parks.fun/fr/attraction/attraction-1/salta",
        Url = "https://amusement-parks.fun/fr/attraction/attraction-1/salta",
        RequestedAtUtc = new DateTime(2026, 11, 27, 21, 37, 0, DateTimeKind.Utc),
        AttemptedAtUtc = new DateTime(2026, 11, 27, 21, 37, 0, DateTimeKind.Utc),
    };

    private static RetryOwnFacebookPublicationCommandHandler CreateHandler(
        InMemorySocialPublicationRepository repository,
        ISocialPublicationService service,
        StubSocialPublisher publisher) => new RetryOwnFacebookPublicationCommandHandler(
            repository,
            service,
            new[] { publisher },
            new SocialPublicationReconciler(repository, NullLogger<SocialPublicationReconciler>.Instance));
}
