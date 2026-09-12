using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
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
    private static readonly DateTime Now = new DateTime(2026, 9, 6, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Publish_ThroughLegacyVisibilityCommand_ShouldRequireAnApprovedPreview()
    {
        Mock<ISharePublicationRepository> repository = new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        ISharePublicationSourceDescriptor source = CreateSourceDescriptor(7);
        SetSharePublicationVisibilityCommandHandler handler = new SetSharePublicationVisibilityCommandHandler(
            repository.Object,
            new[] { source },
            new SharePublicationFixedTimeProvider(Now));

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new SetSharePublicationVisibilityCommand(
                " owner-1 ",
                SharePublicationType.PersonalRanking,
                null,
                true),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.preview-required");
        repository.VerifyNoOtherCalls();
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

    [Fact]
    public async Task GetSettings_WhenApprovedSourceHasChanged_ShouldPresentTheShareAsPrivate()
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
            new[] { CreateSourceDescriptor(8) });

        ApplicationResult<SharePublicationSettingsResult> result = await handler.HandleAsync(
            new GetSharePublicationSettingsQuery(
                OwnerId,
                SharePublicationType.PersonalRanking),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPublic);
        Assert.Null(result.Value.ShareId);
        Assert.Contains(ShareContentField.GlobalRatings, result.Value.IncludedFields);
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
        source.Setup(value => value.GetCurrentSourceVersionAsync(
                It.Is<SharePublicationSourceVersionRequest>(request =>
                    request.SourceScopeKey == ScopeKey),
                CancellationToken.None))
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
