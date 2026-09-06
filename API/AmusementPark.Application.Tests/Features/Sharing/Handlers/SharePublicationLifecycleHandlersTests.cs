using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class SharePublicationLifecycleHandlersTests
{
    private const string OwnerId = "owner-1";
    private const string ScopeKey = "personal-ranking:owner-1";
    private const string TokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string ReplacementTokenValue = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";
    private static readonly DateTime Now = new DateTime(2026, 9, 6, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Publish_WhenNoPublicationExists_ShouldCreateResolvableCentralPublication()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync((SharePublication?)null);
        repository.Setup(value => value.CreateAsync(
                It.Is<SharePublication>(publication =>
                    publication.OwnerUserId == OwnerId
                    && publication.IsResolvable
                    && publication.SourceVersion == 7
                    && publication.ShareToken == ShareToken.Parse(TokenValue)),
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate()).Returns(ShareToken.Parse(TokenValue));
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(7);
        SetSharePublicationVisibilityCommandHandler handler = new SetSharePublicationVisibilityCommandHandler(
            repository.Object,
            new[] { source },
            new SharePublicationPublisher(
                repository.Object,
                tokenFactory.Object,
                new SharePublicationFixedTimeProvider(Now)),
            new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new SetSharePublicationVisibilityCommand(
                " owner-1 ",
                SharePublicationType.PersonalRanking,
                null,
                true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.Equal(TokenValue, result.Value.ShareId);
        Assert.Equal(Now, result.Value.PublishedAtUtc);
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task Revoke_WhenPublicationIsPublic_ShouldCutResolutionAndClearToken()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(publication);
        repository.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Status == SharePublicationStatus.Revoked
                    && candidate.ShareToken == null
                    && !candidate.IsResolvable),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        SetSharePublicationVisibilityCommandHandler handler = new SetSharePublicationVisibilityCommandHandler(
            repository.Object,
            new[] { CreateSourceDescriptor(7) },
            new SharePublicationPublisher(
                repository.Object,
                Mock.Of<IShareTokenFactory>(MockBehavior.Strict),
                new SharePublicationFixedTimeProvider(Now)),
            new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new SetSharePublicationVisibilityCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPublic);
        Assert.Null(result.Value.ShareId);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Publish_AfterRevocation_ShouldCreateANewPublicationAndToken()
    {
        SharePublication revoked = CreatePublishedPublication();
        revoked.Revoke(1, Now.AddHours(-1));
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(revoked);
        repository.Setup(value => value.CreateAsync(
                It.Is<SharePublication>(publication =>
                    publication.Id != revoked.Id
                    && publication.ShareToken == ShareToken.Parse(ReplacementTokenValue)
                    && publication.IsResolvable),
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IShareTokenFactory> tokenFactory = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokenFactory.Setup(value => value.Generate())
            .Returns(ShareToken.Parse(ReplacementTokenValue));
        SetSharePublicationVisibilityCommandHandler handler =
            new SetSharePublicationVisibilityCommandHandler(
                repository.Object,
                new[] { CreateSourceDescriptor(7) },
                new SharePublicationPublisher(
                    repository.Object,
                    tokenFactory.Object,
                    new SharePublicationFixedTimeProvider(Now)),
                new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new SetSharePublicationVisibilityCommand(
                OwnerId,
                SharePublicationType.PersonalRanking,
                null,
                true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReplacementTokenValue, result.Value!.ShareId);
        Assert.NotEqual(TokenValue, result.Value.ShareId);
        repository.VerifyAll();
        tokenFactory.VerifyAll();
    }

    [Fact]
    public async Task GetSettings_ShouldReadTheCentralPublicationByTypedSource()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetOwnedBySourceAsync(
                OwnerId,
                SharePublicationType.PersonalRanking,
                ScopeKey,
                CancellationToken.None))
            .ReturnsAsync(publication);
        GetSharePublicationSettingsQueryHandler handler = new GetSharePublicationSettingsQueryHandler(
            repository.Object,
            new[] { CreateSourceDescriptor(7) });

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new GetSharePublicationSettingsQuery(
                OwnerId,
                SharePublicationType.PersonalRanking),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.Equal(TokenValue, result.Value.ShareId);
        repository.VerifyAll();
    }

    private static ISharePublicationSourceDescriptor CreateSourceDescriptor(long sourceVersion)
    {
        Mock<ISharePublicationSourceDescriptor> source =
            new Mock<ISharePublicationSourceDescriptor>(MockBehavior.Strict);
        source.SetupGet(value => value.PublicationType)
            .Returns(SharePublicationType.PersonalRanking);
        source.Setup(value => value.ResolveSourceScopeKey(OwnerId, null))
            .Returns(ApplicationResult<string>.Success(ScopeKey));
        source.Setup(value => value.CreateDefaultPolicy())
            .Returns(CreatePolicy());
        source.Setup(value => value.GetCurrentSourceVersionAsync(ScopeKey, CancellationToken.None))
            .ReturnsAsync(ApplicationResult<long>.Success(sourceVersion));
        return source.Object;
    }

    private static SharePublication CreatePublishedPublication()
    {
        return SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            OwnerId,
            SharePublicationType.PersonalRanking,
            ScopeKey,
            ShareToken.Parse(TokenValue),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            CreatePolicy(),
            7,
            1,
            1,
            Now.AddDays(-1),
            null,
            Now.AddDays(-2),
            Now.AddDays(-1));
    }

    private static ShareContentPolicy CreatePolicy()
    {
        return ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.GlobalRatings,
            });
    }
}
