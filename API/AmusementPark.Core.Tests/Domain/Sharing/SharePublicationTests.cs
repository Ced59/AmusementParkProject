using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class SharePublicationTests
{
    private static readonly DateTime InitialUtc =
        new DateTime(2026, 9, 5, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldInitializeANonResolvablePrivateDraft()
    {
        SharePublication publication = CreatePublication();

        Assert.Equal("publication-1", publication.Id.Value);
        Assert.Equal("user-1", publication.OwnerUserId);
        Assert.Equal("visit:user-1:visit-1", publication.SourceScopeKey);
        Assert.Equal(SharePublicationType.VisitRecap, publication.Type);
        Assert.Equal(SharePublicationStatus.Draft, publication.Status);
        Assert.Equal(ShareVisibility.Private, publication.Visibility);
        Assert.Null(publication.ShareToken);
        Assert.Equal(4, publication.SourceVersion);
        Assert.Equal(0, publication.PublicationVersion);
        Assert.False(publication.IsResolvable);
        Assert.Null(publication.PublishedAtUtc);
        Assert.Null(publication.RevokedAtUtc);
        Assert.Equal(InitialUtc, publication.CreatedAtUtc);
        Assert.Equal(InitialUtc, publication.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenPolicyTargetsAnotherType_ShouldRejectIt()
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(
            SharePublicationType.YearRecap);

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => CreatePublication(policy: policy));

        Assert.Equal(SharePublicationErrorCodes.PolicyTypeMismatch, exception.ErrorCode);
    }

    [Fact]
    public void Publish_AfterApprovedPreview_ShouldExposeOnlyTheOpaqueLink()
    {
        SharePublication publication = CreatePublication();
        DateTime publishedAtUtc = InitialUtc.AddMinutes(1);

        publication.Publish(
            "  opaque-token  ",
            ShareVisibility.Unlisted,
            approvedSourceVersion: 4,
            approvedContentPolicy: publication.ContentPolicy,
            expectedPublicationVersion: 0,
            publishedAtUtc);

        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.Equal(ShareVisibility.Unlisted, publication.Visibility);
        Assert.Equal("opaque-token", publication.ShareToken);
        Assert.Equal(1, publication.PublicationVersion);
        Assert.True(publication.IsResolvable);
        Assert.Equal(publishedAtUtc, publication.PublishedAtUtc);
        Assert.Equal(publishedAtUtc, publication.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("policy")]
    [InlineData("publication")]
    public void Publish_WhenApprovedPreviewIsStale_ShouldRejectItWithoutMutation(string stalePart)
    {
        SharePublication publication = CreatePublication();
        long sourceVersion = stalePart == "source" ? 3 : 4;
        ShareContentPolicy approvedPolicy = stalePart == "policy"
            ? ShareContentPolicy.Create(
                SharePublicationType.VisitRecap,
                ShareDatePrecision.Year,
                Array.Empty<ShareContentField>())
            : publication.ContentPolicy;
        long publicationVersion = stalePart == "publication" ? 1 : 0;

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.Publish(
                "opaque-token",
                ShareVisibility.Unlisted,
                sourceVersion,
                approvedPolicy,
                publicationVersion,
                InitialUtc.AddMinutes(1)));

        string expectedErrorCode = stalePart switch
        {
            "source" => SharePublicationErrorCodes.PreviewSourceVersionMismatch,
            "policy" => SharePublicationErrorCodes.PreviewPolicyMismatch,
            _ => SharePublicationErrorCodes.PublicationVersionConflict,
        };
        Assert.Equal(expectedErrorCode, exception.ErrorCode);
        Assert.Equal(SharePublicationStatus.Draft, publication.Status);
        Assert.Equal(0, publication.PublicationVersion);
        Assert.Null(publication.ShareToken);
    }

    [Fact]
    public void Publish_WhenVisibilityRemainsPrivate_ShouldRejectIt()
    {
        SharePublication publication = CreatePublication();

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.Publish(
                "opaque-token",
                ShareVisibility.Private,
                4,
                publication.ContentPolicy,
                0,
                InitialUtc.AddMinutes(1)));

        Assert.Equal(SharePublicationErrorCodes.PublicVisibilityRequired, exception.ErrorCode);
    }

    [Fact]
    public void MarkSourceChanged_WhenPublished_ShouldSuspendResolutionUntilReview()
    {
        SharePublication publication = CreatePublishedPublication();

        publication.MarkSourceChanged(5, InitialUtc.AddMinutes(2));

        Assert.Equal(SharePublicationStatus.NeedsReview, publication.Status);
        Assert.Equal(ShareVisibility.Private, publication.Visibility);
        Assert.Equal("opaque-token", publication.ShareToken);
        Assert.Equal(5, publication.SourceVersion);
        Assert.Equal(2, publication.PublicationVersion);
        Assert.False(publication.IsResolvable);
    }

    [Fact]
    public void MarkSourceChanged_WhenRevisionIsReplayed_ShouldRemainIdempotent()
    {
        SharePublication publication = CreatePublishedPublication();

        publication.MarkSourceChanged(4, InitialUtc.AddMinutes(2));

        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.Equal(1, publication.PublicationVersion);
        Assert.Equal(InitialUtc.AddMinutes(1), publication.UpdatedAtUtc);
    }

    [Fact]
    public void MarkSourceChanged_WhenRevisionMovesBackwards_ShouldRejectIt()
    {
        SharePublication publication = CreatePublishedPublication();

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.MarkSourceChanged(3, InitialUtc.AddMinutes(2)));

        Assert.Equal(SharePublicationErrorCodes.InvalidSourceVersion, exception.ErrorCode);
        Assert.True(publication.IsResolvable);
    }

    [Fact]
    public void ReplaceContentPolicy_WhenPublished_ShouldSuspendResolution()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareContentPolicy nextPolicy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount });

        publication.ReplaceContentPolicy(nextPolicy, 1, InitialUtc.AddMinutes(2));

        Assert.Same(nextPolicy, publication.ContentPolicy);
        Assert.Equal(SharePublicationStatus.NeedsReview, publication.Status);
        Assert.Equal(ShareVisibility.Private, publication.Visibility);
        Assert.Equal(2, publication.PublicationVersion);
        Assert.False(publication.IsResolvable);
    }

    [Fact]
    public void ReplaceContentPolicy_WhenSelectionIsEquivalent_ShouldNotCreateAFakeRevision()
    {
        ShareContentPolicy initialPolicy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.Avatar, ShareContentField.RideCount });
        SharePublication publication = CreatePublishedPublication(initialPolicy);
        ShareContentPolicy equivalentPolicy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount, ShareContentField.Avatar });

        publication.ReplaceContentPolicy(equivalentPolicy, 1, InitialUtc.AddMinutes(2));

        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.Equal(1, publication.PublicationVersion);
        Assert.Same(initialPolicy, publication.ContentPolicy);
    }

    [Fact]
    public void Publish_AfterReview_ShouldCreateANewPublicRevision()
    {
        SharePublication publication = CreatePublishedPublication();
        publication.MarkSourceChanged(5, InitialUtc.AddMinutes(2));

        publication.Publish(
            "opaque-token",
            ShareVisibility.Public,
            approvedSourceVersion: 5,
            approvedContentPolicy: publication.ContentPolicy,
            expectedPublicationVersion: 2,
            InitialUtc.AddMinutes(3));

        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.Equal(ShareVisibility.Public, publication.Visibility);
        Assert.Equal(3, publication.PublicationVersion);
        Assert.True(publication.IsResolvable);
    }

    [Fact]
    public void RotateToken_ShouldInvalidateTheOldLinkAndIncrementThePublicRevision()
    {
        SharePublication publication = CreatePublishedPublication();

        publication.RotateToken("new-token", 1, InitialUtc.AddMinutes(2));

        Assert.Equal("new-token", publication.ShareToken);
        Assert.Equal(2, publication.PublicationVersion);
        Assert.True(publication.IsResolvable);
    }

    [Fact]
    public void RotateToken_WhenTokenDoesNotChange_ShouldRejectIt()
    {
        SharePublication publication = CreatePublishedPublication();

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.RotateToken(" opaque-token ", 1, InitialUtc.AddMinutes(2)));

        Assert.Equal(SharePublicationErrorCodes.ShareTokenUnchanged, exception.ErrorCode);
        Assert.Equal(1, publication.PublicationVersion);
    }

    [Fact]
    public void Revoke_ShouldImmediatelyRemoveTheTokenAndBecomeTerminal()
    {
        SharePublication publication = CreatePublishedPublication();
        DateTime revokedAtUtc = InitialUtc.AddMinutes(2);

        publication.Revoke(1, revokedAtUtc);

        Assert.Equal(SharePublicationStatus.Revoked, publication.Status);
        Assert.Equal(ShareVisibility.Private, publication.Visibility);
        Assert.Null(publication.ShareToken);
        Assert.Equal(2, publication.PublicationVersion);
        Assert.Equal(revokedAtUtc, publication.RevokedAtUtc);
        Assert.False(publication.IsResolvable);

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.MarkSourceChanged(5, revokedAtUtc.AddMinutes(1)));
        Assert.Equal(SharePublicationErrorCodes.InvalidTransition, exception.ErrorCode);
    }

    [Fact]
    public void Revoke_WhenCommandIsReplayedAtTheCurrentVersion_ShouldRemainIdempotent()
    {
        SharePublication publication = CreatePublishedPublication();
        publication.Revoke(1, InitialUtc.AddMinutes(2));

        publication.Revoke(2, InitialUtc.AddMinutes(3));

        Assert.Equal(2, publication.PublicationVersion);
        Assert.Equal(InitialUtc.AddMinutes(2), publication.UpdatedAtUtc);
        Assert.Equal(InitialUtc.AddMinutes(2), publication.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_WhenPublicationIsStillDraft_ShouldRejectIt()
    {
        SharePublication publication = CreatePublication();

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.Revoke(0, InitialUtc.AddMinutes(1)));

        Assert.Equal(SharePublicationErrorCodes.InvalidTransition, exception.ErrorCode);
    }

    [Theory]
    [InlineData(SharePublicationStatus.Draft, ShareVisibility.Unlisted, null, 0, false, false)]
    [InlineData(SharePublicationStatus.Published, ShareVisibility.Private, "token", 1, true, false)]
    [InlineData(SharePublicationStatus.Published, ShareVisibility.Unlisted, null, 1, true, false)]
    [InlineData(SharePublicationStatus.NeedsReview, ShareVisibility.Private, null, 1, true, false)]
    [InlineData(SharePublicationStatus.Revoked, ShareVisibility.Private, "token", 2, true, true)]
    public void Restore_WhenLifecycleStateIsInconsistent_ShouldRejectIt(
        SharePublicationStatus status,
        ShareVisibility visibility,
        string? shareToken,
        long publicationVersion,
        bool hasPublishedAt,
        bool hasRevokedAt)
    {
        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => RestorePublication(
                status,
                visibility,
                shareToken,
                publicationVersion,
                hasPublishedAt ? InitialUtc.AddMinutes(1) : null,
                hasRevokedAt ? InitialUtc.AddMinutes(2) : null,
                hasRevokedAt ? InitialUtc.AddMinutes(2) : InitialUtc.AddMinutes(1)));

        Assert.Equal(SharePublicationErrorCodes.InvalidRestoredState, exception.ErrorCode);
    }

    [Fact]
    public void Restore_WhenPublishedStateIsConsistent_ShouldRemainResolvable()
    {
        SharePublication publication = RestorePublication(
            SharePublicationStatus.Published,
            ShareVisibility.Public,
            "token",
            3,
            InitialUtc.AddMinutes(1),
            null,
            InitialUtc.AddMinutes(2));

        Assert.True(publication.IsResolvable);
        Assert.Equal(3, publication.PublicationVersion);
    }

    [Fact]
    public void Create_WhenTimestampIsNotUtcOrSourceVersionIsNegative_ShouldRejectIt()
    {
        SharePublicationValidationException timestamp = Assert.Throws<SharePublicationValidationException>(
            () => SharePublication.Create(
                SharePublicationId.Parse("publication-1"),
                "user-1",
                SharePublicationType.VisitRecap,
                "visit:user-1:visit-1",
                ShareContentPolicy.CreatePrivateDefault(SharePublicationType.VisitRecap),
                4,
                DateTime.SpecifyKind(InitialUtc, DateTimeKind.Local)));
        SharePublicationValidationException version = Assert.Throws<SharePublicationValidationException>(
            () => CreatePublication(sourceVersion: -1));

        Assert.Equal(SharePublicationErrorCodes.TimestampNotUtc, timestamp.ErrorCode);
        Assert.Equal(SharePublicationErrorCodes.InvalidSourceVersion, version.ErrorCode);
    }

    [Fact]
    public void Mutation_WhenTimestampMovesBackwards_ShouldRejectItWithoutMutation()
    {
        SharePublication publication = CreatePublishedPublication();

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.RotateToken("new-token", 1, InitialUtc));

        Assert.Equal(SharePublicationErrorCodes.InvalidTimestampOrder, exception.ErrorCode);
        Assert.Equal("opaque-token", publication.ShareToken);
        Assert.Equal(1, publication.PublicationVersion);
    }

    [Fact]
    public void Mutation_WhenPublicationVersionCannotAdvance_ShouldRejectWithoutPartialChanges()
    {
        SharePublication publication = RestorePublication(
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            "opaque-token",
            long.MaxValue,
            InitialUtc.AddMinutes(1),
            null,
            InitialUtc.AddMinutes(1));

        SharePublicationValidationException exception = Assert.Throws<SharePublicationValidationException>(
            () => publication.MarkSourceChanged(5, InitialUtc.AddMinutes(2)));

        Assert.Equal(SharePublicationErrorCodes.PublicationVersionOverflow, exception.ErrorCode);
        Assert.Equal(4, publication.SourceVersion);
        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.True(publication.IsResolvable);
    }

    private static SharePublication CreatePublication(
        ShareContentPolicy? policy = null,
        long sourceVersion = 4)
    {
        return SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            " user-1 ",
            SharePublicationType.VisitRecap,
            " visit:user-1:visit-1 ",
            policy ?? ShareContentPolicy.CreatePrivateDefault(SharePublicationType.VisitRecap),
            sourceVersion,
            InitialUtc);
    }

    private static SharePublication CreatePublishedPublication(ShareContentPolicy? policy = null)
    {
        SharePublication publication = CreatePublication(policy);
        publication.Publish(
            "opaque-token",
            ShareVisibility.Unlisted,
            4,
            publication.ContentPolicy,
            0,
            InitialUtc.AddMinutes(1));
        return publication;
    }

    private static SharePublication RestorePublication(
        SharePublicationStatus status,
        ShareVisibility visibility,
        string? shareToken,
        long publicationVersion,
        DateTime? publishedAtUtc,
        DateTime? revokedAtUtc,
        DateTime updatedAtUtc)
    {
        return SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            "user-1",
            SharePublicationType.VisitRecap,
            "visit:user-1:visit-1",
            shareToken,
            status,
            visibility,
            ShareContentPolicy.CreatePrivateDefault(SharePublicationType.VisitRecap),
            sourceVersion: 4,
            publicationVersion,
            publishedAtUtc,
            revokedAtUtc,
            InitialUtc,
            updatedAtUtc);
    }
}
