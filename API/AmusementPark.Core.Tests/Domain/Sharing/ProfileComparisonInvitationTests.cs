using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ProfileComparisonInvitationTests
{
    private const string CreatorUserId = "creator-1";
    private const string AcceptorUserId = "acceptor-1";
    private const string Token = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizeCategoriesAndRemainPending()
    {
        ProfileComparisonInvitation invitation = Create(
            new[]
            {
                ProfileComparisonCategory.PersonalRatings,
                ProfileComparisonCategory.VisitedParks,
                ProfileComparisonCategory.PersonalRatings,
            });

        Assert.Equal(
            new[]
            {
                ProfileComparisonCategory.VisitedParks,
                ProfileComparisonCategory.PersonalRatings,
            },
            invitation.Categories);
        Assert.Equal(ProfileComparisonInvitationStatus.Pending, invitation.Status);
        Assert.False(invitation.IsAccepted);
        Assert.Equal(0, invitation.Version);
    }

    [Fact]
    public void Accept_ShouldRecordBothPassportConsentsAndIncrementVersion()
    {
        ProfileComparisonInvitation invitation = Create(
            new[] { ProfileComparisonCategory.VisitedParks });
        SharePublicationId acceptorPassportId = SharePublicationId.New();
        ProfileComparisonId comparisonId = ProfileComparisonId.New();

        invitation.Accept(
            AcceptorUserId,
            acceptorPassportId,
            4,
            comparisonId,
            NowUtc.AddHours(1));

        Assert.True(invitation.IsAccepted);
        Assert.Equal(AcceptorUserId, invitation.AcceptorUserId);
        Assert.Equal(acceptorPassportId, invitation.AcceptorPassportPublicationId);
        Assert.Equal(4, invitation.AcceptorPassportPublicationVersion);
        Assert.Equal(comparisonId, invitation.ComparisonId);
        Assert.Equal(NowUtc.AddHours(1), invitation.AcceptedAtUtc);
        Assert.Equal(1, invitation.Version);
    }

    [Fact]
    public void Accept_WhenExpired_ShouldRejectTheConsent()
    {
        ProfileComparisonInvitation invitation = Create(
            new[] { ProfileComparisonCategory.VisitedParks });

        ProfileComparisonInvitationValidationException exception = Assert.Throws<
            ProfileComparisonInvitationValidationException>(() => invitation.Accept(
                AcceptorUserId,
                SharePublicationId.New(),
                1,
                ProfileComparisonId.New(),
                invitation.ExpiresAtUtc));

        Assert.Equal(ProfileComparisonInvitationErrorCodes.Expired, exception.ErrorCode);
        Assert.False(invitation.IsAccepted);
    }

    [Fact]
    public void Accept_WhenCreatorAccepts_ShouldRejectTheConsent()
    {
        ProfileComparisonInvitation invitation = Create(
            new[] { ProfileComparisonCategory.VisitedParks });

        ProfileComparisonInvitationValidationException exception = Assert.Throws<
            ProfileComparisonInvitationValidationException>(() => invitation.Accept(
                CreatorUserId,
                SharePublicationId.New(),
                1,
                ProfileComparisonId.New(),
                NowUtc.AddHours(1)));

        Assert.Equal(ProfileComparisonInvitationErrorCodes.SelfAcceptance, exception.ErrorCode);
        Assert.False(invitation.IsAccepted);
    }

    [Fact]
    public void Create_WithoutCategory_ShouldRejectTheInvitation()
    {
        ProfileComparisonInvitationValidationException exception = Assert.Throws<
            ProfileComparisonInvitationValidationException>(() => Create(
                Array.Empty<ProfileComparisonCategory>()));

        Assert.Equal(
            ProfileComparisonInvitationErrorCodes.InvalidCategoryCount,
            exception.ErrorCode);
    }

    [Fact]
    public void Restore_WhenAcceptedAfterExpiry_ShouldRejectTheState()
    {
        SharePublicationId creatorPassportId = SharePublicationId.New();
        SharePublicationId acceptorPassportId = SharePublicationId.New();
        DateTime expiresAtUtc = NowUtc.AddDays(7);

        ProfileComparisonInvitationValidationException exception = Assert.Throws<
            ProfileComparisonInvitationValidationException>(() =>
                ProfileComparisonInvitation.Restore(
                    ProfileComparisonInvitationId.New(),
                    ShareToken.Parse(Token),
                    CreatorUserId,
                    creatorPassportId,
                    3,
                    new[] { ProfileComparisonCategory.VisitedParks },
                    ProfileComparisonInvitationStatus.Accepted,
                    AcceptorUserId,
                    acceptorPassportId,
                    4,
                    ProfileComparisonId.New(),
                    expiresAtUtc,
                    expiresAtUtc,
                    NowUtc,
                    expiresAtUtc,
                    1));

        Assert.Equal(ProfileComparisonInvitationErrorCodes.InvalidState, exception.ErrorCode);
    }

    private static ProfileComparisonInvitation Create(
        IEnumerable<ProfileComparisonCategory> categories)
    {
        return ProfileComparisonInvitation.Create(
            ProfileComparisonInvitationId.New(),
            ShareToken.Parse(Token),
            CreatorUserId,
            SharePublicationId.New(),
            3,
            categories,
            NowUtc,
            NowUtc.AddDays(7));
    }
}
