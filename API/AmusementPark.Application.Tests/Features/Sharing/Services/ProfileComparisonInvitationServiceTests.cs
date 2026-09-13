using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ProfileComparisonInvitationServiceTests
{
    private const string CreatorId = "creator-1";
    private const string InviteeId = "invitee-1";
    private const string Token = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ProfileComparisonCategory[] Categories =
        new[] { ProfileComparisonCategory.VisitedParks };

    [Fact]
    public async Task CreateAsync_WithComparablePublicPassport_ShouldCreateSevenDayInvitation()
    {
        SharePublication creator = CreatePublishedPassport(CreatorId, "creator-passport");
        PassportProfileShareSnapshot creatorSnapshot = CreateSnapshot(creator, "Camille", true);
        Mock<IProfileComparisonInvitationRepository> invitations =
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict);
        invitations.Setup(value => value.CreateAsync(
                It.Is<ProfileComparisonInvitation>(invitation =>
                    invitation.CreatorUserId == CreatorId
                    && invitation.CreatorPassportPublicationId == creator.Id
                    && invitation.ExpiresAtUtc == NowUtc.AddDays(7)),
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonInvitationWriteOutcome.Success);
        ProfileComparisonInvitationService service = CreateService(
            invitations.Object,
            CreatePublicationRepository(creator).Object,
            CreateSnapshotRepository(creatorSnapshot).Object,
            availablePublications: new[] { creator });

        ApplicationResult<ProfileComparisonInvitationCreationResult> result =
            await service.CreateAsync(CreatorId, Categories, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Token, result.Value!.Token);
        Assert.Equal(NowUtc.AddDays(7), result.Value.ExpiresAtUtc);
        invitations.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_WhenComparisonsAreDisabled_ShouldRejectInvitation()
    {
        SharePublication creator = CreatePublishedPassport(CreatorId, "creator-passport");
        PassportProfileShareSnapshot snapshot = CreateSnapshot(creator, "Camille", false);
        ProfileComparisonInvitationService service = CreateService(
            Mock.Of<IProfileComparisonInvitationRepository>(MockBehavior.Strict),
            CreatePublicationRepository(creator).Object,
            CreateSnapshotRepository(snapshot).Object,
            availablePublications: new[] { creator });

        ApplicationResult<ProfileComparisonInvitationCreationResult> result =
            await service.CreateAsync(CreatorId, Categories, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "profile-comparison.passport-unavailable");
    }

    [Fact]
    public async Task PreviewAsync_WhenCreatorRotatedPassport_ShouldInvalidateInvitation()
    {
        SharePublication creator = CreatePublishedPassport(CreatorId, "creator-passport");
        ProfileComparisonInvitation invitation = CreateInvitation(creator);
        creator.RotateToken(
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhQ"),
            1,
            NowUtc.AddMinutes(1));
        Mock<IProfileComparisonInvitationRepository> invitations =
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict);
        invitations.Setup(value => value.GetByTokenAsync(
                invitation.Token,
                CancellationToken.None))
            .ReturnsAsync(invitation);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetOwnedAsync(
                creator.Id,
                CreatorId,
                CancellationToken.None))
            .ReturnsAsync(creator);
        ProfileComparisonInvitationService service = CreateService(
            invitations.Object,
            publications.Object,
            Mock.Of<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict));

        ApplicationResult<ProfileComparisonInvitationPreviewResult> result =
            await service.PreviewAsync(InviteeId, Token, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ProfileComparisonInvitationPreviewStatus.InviterPassportUnavailable,
            result.Value!.Status);
        Assert.False(result.Value.CanAccept);
    }

    [Fact]
    public async Task AcceptAsync_WithTwoCurrentPassports_ShouldPersistBothConsents()
    {
        SharePublication creator = CreatePublishedPassport(CreatorId, "creator-passport");
        SharePublication invitee = CreatePublishedPassport(InviteeId, "invitee-passport");
        PassportProfileShareSnapshot creatorSnapshot = CreateSnapshot(creator, "Camille", true);
        PassportProfileShareSnapshot inviteeSnapshot = CreateSnapshot(invitee, "Alex", true);
        ProfileComparisonInvitation invitation = CreateInvitation(creator);
        Mock<IProfileComparisonInvitationRepository> invitations =
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict);
        invitations.Setup(value => value.GetByTokenAsync(
                invitation.Token,
                CancellationToken.None))
            .ReturnsAsync(invitation);
        invitations.Setup(value => value.ReplaceAsync(
                It.Is<ProfileComparisonInvitation>(candidate =>
                    candidate.IsAccepted
                    && candidate.AcceptorUserId == InviteeId
                    && candidate.AcceptorPassportPublicationId == invitee.Id),
                0,
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonInvitationWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            CreatePublicationRepository(creator, invitee);
        Mock<IPassportProfileShareSnapshotRepository> snapshots =
            CreateSnapshotRepository(creatorSnapshot, inviteeSnapshot);
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        comparisons.Setup(value => value.GetByIdAsync(
                It.IsAny<ProfileComparisonId>(),
                CancellationToken.None))
            .ReturnsAsync((ProfileComparison?)null);
        comparisons.Setup(value => value.CreateAsync(
                It.Is<ProfileComparison>(comparison =>
                    comparison.CreatorUserId == CreatorId
                    && comparison.AcceptorUserId == InviteeId),
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonWriteOutcome.Success);
        ProfileComparisonInvitationService service = CreateService(
            invitations.Object,
            publications.Object,
            snapshots.Object,
            comparisons.Object,
            new[] { creator, invitee });

        ApplicationResult<ProfileComparisonInvitationAcceptanceResult> result =
            await service.AcceptAsync(InviteeId, Token, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Token, result.Value!.ShareId);
        Assert.Equal(NowUtc, result.Value.AcceptedAtUtc);
        invitations.VerifyAll();
    }

    [Fact]
    public async Task AcceptAsync_WhenCreatorPassportChangesDuringAcceptance_ShouldRemoveInvalidConsent()
    {
        SharePublication creator = CreatePublishedPassport(CreatorId, "creator-passport");
        SharePublication invitee = CreatePublishedPassport(InviteeId, "invitee-passport");
        PassportProfileShareSnapshot creatorSnapshot = CreateSnapshot(creator, "Camille", true);
        PassportProfileShareSnapshot inviteeSnapshot = CreateSnapshot(invitee, "Alex", true);
        ProfileComparisonInvitation invitation = CreateInvitation(creator);
        Mock<IProfileComparisonInvitationRepository> invitations =
            new Mock<IProfileComparisonInvitationRepository>(MockBehavior.Strict);
        invitations.Setup(value => value.GetByTokenAsync(
                invitation.Token,
                CancellationToken.None))
            .ReturnsAsync(invitation);
        invitations.Setup(value => value.ReplaceAsync(
                It.Is<ProfileComparisonInvitation>(candidate => candidate.IsAccepted),
                0,
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonInvitationWriteOutcome.Success);
        invitations.Setup(value => value.DeleteAcceptedAsync(
                invitation.Id,
                1,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<ISharePublicationRepository> publications =
            CreatePublicationRepository(creator, invitee);
        publications.SetupSequence(value => value.GetOwnedAsync(
                creator.Id,
                CreatorId,
                CancellationToken.None))
            .ReturnsAsync(creator)
            .ReturnsAsync((SharePublication?)null);
        ProfileComparisonInvitationService service = CreateService(
            invitations.Object,
            publications.Object,
            CreateSnapshotRepository(creatorSnapshot, inviteeSnapshot).Object,
            availablePublications: new[] { creator, invitee });

        ApplicationResult<ProfileComparisonInvitationAcceptanceResult> result =
            await service.AcceptAsync(InviteeId, Token, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "profile-comparison.invitation-not-acceptable");
        invitations.VerifyAll();
    }

    private static ProfileComparisonInvitationService CreateService(
        IProfileComparisonInvitationRepository invitations,
        ISharePublicationRepository publications,
        IPassportProfileShareSnapshotRepository snapshots,
        IProfileComparisonRepository? comparisons = null,
        IReadOnlyCollection<SharePublication>? availablePublications = null)
    {
        Mock<IShareTokenFactory> tokens = new Mock<IShareTokenFactory>(MockBehavior.Strict);
        tokens.Setup(value => value.Generate()).Returns(ShareToken.Parse(Token));
        ProfileComparisonPassportResolver resolver =
            new ProfileComparisonPassportResolver(
                publications,
                snapshots,
                CreateAccessResolver(availablePublications ?? Array.Empty<SharePublication>()).Object);
        ProfileComparisonMaterializer materializer = new ProfileComparisonMaterializer(
            comparisons ?? Mock.Of<IProfileComparisonRepository>(MockBehavior.Strict),
            resolver,
            tokens.Object,
            new SharePublicationFixedTimeProvider(NowUtc));
        return new ProfileComparisonInvitationService(
            invitations,
            resolver,
            materializer,
            tokens.Object,
            new SharePublicationFixedTimeProvider(NowUtc));
    }

    private static Mock<ISharePublicationRepository> CreatePublicationRepository(
        params SharePublication[] publications)
    {
        Mock<ISharePublicationRepository> repository =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        foreach (SharePublication publication in publications)
        {
            repository.Setup(value => value.GetOwnedBySourceAsync(
                    publication.OwnerUserId,
                    SharePublicationType.PassportProfile,
                    PassportProfileShareSourceScope.Create(publication.OwnerUserId),
                    CancellationToken.None))
                .ReturnsAsync(publication);
            repository.Setup(value => value.GetOwnedAsync(
                    publication.Id,
                    publication.OwnerUserId,
                    CancellationToken.None))
                .ReturnsAsync(publication);
        }

        return repository;
    }

    private static Mock<IPassportProfileShareSnapshotRepository> CreateSnapshotRepository(
        params PassportProfileShareSnapshot[] snapshots)
    {
        Mock<IPassportProfileShareSnapshotRepository> repository =
            new Mock<IPassportProfileShareSnapshotRepository>(MockBehavior.Strict);
        foreach (PassportProfileShareSnapshot snapshot in snapshots)
        {
            repository.Setup(value => value.GetAsync(
                    snapshot.PublicationId,
                    snapshot.PublicationVersion,
                    CancellationToken.None))
                .ReturnsAsync(snapshot);
        }

        return repository;
    }

    private static Mock<ISharePublicationAccessResolver> CreateAccessResolver(
        IEnumerable<SharePublication> publications)
    {
        Mock<ISharePublicationAccessResolver> resolver =
            new Mock<ISharePublicationAccessResolver>(MockBehavior.Strict);
        foreach (SharePublication publication in publications)
        {
            resolver.Setup(value => value.ResolveAsync(
                    publication.ShareToken!.Value.Value,
                    SharePublicationType.PassportProfile,
                    CancellationToken.None))
                .ReturnsAsync(ApplicationResult<ResolvedSharePublicationResult>.Success(
                    new ResolvedSharePublicationResult(
                        publication.OwnerUserId,
                        null,
                        publication.Type,
                        publication.ContentPolicy,
                        publication.PublishedAtUtc!.Value,
                        publication.SourceScopeKey,
                        publication.SourceVersion,
                        publication.PublicationVersion,
                        publication.Id.Value,
                        publication.ContentFingerprint)));
        }

        return resolver;
    }

    private static SharePublication CreatePublishedPassport(string userId, string id)
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[] { ShareContentField.GeographicStatistics });
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse(id),
            userId,
            SharePublicationType.PassportProfile,
            PassportProfileShareSourceScope.Create(userId),
            policy,
            9,
            NowUtc,
            "fingerprint");
        publication.Publish(
            ShareToken.Parse(userId == CreatorId
                ? "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhA"
                : "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhQ"),
            ShareVisibility.Unlisted,
            9,
            policy,
            0,
            NowUtc,
            "fingerprint");
        return publication;
    }

    private static PassportProfileShareSnapshot CreateSnapshot(
        SharePublication publication,
        string displayName,
        bool allowsComparisons)
    {
        PassportProfileShareInput selection = new PassportProfileShareInput(
            Array.Empty<int>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            allowsComparisons);
        PassportProfileSharePreviewResult content = new PassportProfileSharePreviewResult(
            displayName,
            null,
            null,
            ShareVisibility.Unlisted,
            allowsComparisons,
            1,
            1,
            null,
            null,
            null,
            null,
            Array.Empty<PassportProfileShareCountryResult>(),
            Array.Empty<PassportProfileShareYearResult>(),
            Array.Empty<PassportProfileShareParkResult>(),
            Array.Empty<PassportProfileShareRatingResult>(),
            Array.Empty<PassportProfileShareMissedItemResult>(),
            false,
            "passport-profile-v1",
            false);
        return new PassportProfileShareSnapshot(
            publication.Id,
            publication.PublicationVersion,
            publication.Version,
            publication.SourceVersion,
            publication.ContentPolicy.SchemaVersion,
            publication.ContentPolicy.DatePrecision,
            publication.ContentPolicy.IncludedFields,
            publication.ContentFingerprint,
            selection,
            content,
            NowUtc);
    }

    private static ProfileComparisonInvitation CreateInvitation(SharePublication creator)
    {
        return ProfileComparisonInvitation.Create(
            ProfileComparisonInvitationId.New(),
            ShareToken.Parse(Token),
            CreatorId,
            creator.Id,
            creator.PublicationVersion,
            Categories,
            NowUtc,
            NowUtc.AddDays(7));
    }
}
