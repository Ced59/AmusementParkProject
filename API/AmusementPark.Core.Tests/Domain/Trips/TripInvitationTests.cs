using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripInvitationTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldExposeOnlyTheBoundedInvitationPreview()
    {
        TripInvitation invitation = CreateInvitation();

        Assert.Equal("Voyage été", invitation.TripTitle);
        Assert.Equal(TripDelegatedRole.Participant, invitation.ProposedRole);
        Assert.Equal(TripInvitationPreviewPolicy.ApproximatePeriod, invitation.PreviewPolicy);
        Assert.Equal(TripInvitationPeriodKind.MonthRange, invitation.PeriodPreview.Kind);
        Assert.Equal("2027-07", invitation.PeriodPreview.StartMonth);
        Assert.Equal("2027-08", invitation.PeriodPreview.EndMonth);
        Assert.Equal(TripInvitationMemberCountBand.TwoToFive, invitation.MemberCountBand);
        Assert.True(invitation.IsTargeted);
        Assert.True(invitation.IsPubliclyResolvable(CreatedAtUtc.AddHours(1)));
        Assert.False(invitation.IsPubliclyResolvable(invitation.ExpiresAtUtc));
    }

    [Fact]
    public void Revoke_ShouldInvalidateTheLinkAndAdvanceItsVersion()
    {
        TripInvitation invitation = CreateInvitation();
        DateTime revokedAtUtc = CreatedAtUtc.AddHours(2);

        invitation.Revoke(revokedAtUtc);

        Assert.Equal(TripInvitationStatus.Revoked, invitation.Status);
        Assert.Equal(revokedAtUtc, invitation.RevokedAtUtc);
        Assert.Equal(2, invitation.Version);
        Assert.False(invitation.IsPubliclyResolvable(revokedAtUtc));
    }

    [Theory]
    [InlineData(1, TripInvitationMemberCountBand.One)]
    [InlineData(2, TripInvitationMemberCountBand.TwoToFive)]
    [InlineData(6, TripInvitationMemberCountBand.SixToTen)]
    [InlineData(11, TripInvitationMemberCountBand.ElevenToFifty)]
    public void ResolveMemberCountBand_ShouldAvoidExposingAnExactGroupSize(
        int memberCount,
        TripInvitationMemberCountBand expected)
    {
        Assert.Equal(expected, TripInvitation.ResolveMemberCountBand(memberCount));
    }

    [Fact]
    public void Restore_ShouldRejectAnExactOrMalformedPeriodPreview()
    {
        Assert.Throws<TripInvitationValidationException>(() =>
            TripInvitationPeriodPreview.Restore(
                TripInvitationPeriodKind.SingleMonth,
                "2027-07",
                "2027-08"));
    }

    private static TripInvitation CreateInvitation()
    {
        return TripInvitation.Create(
            TripInvitationId.Parse("invitation-1"),
            TripPlanId.Parse("trip-1"),
            " Voyage été ",
            "hashed-token",
            "hint42",
            TripDelegatedRole.Participant,
            TripMemberId.Parse("member-1"),
            "Camille",
            "email-hmac",
            "v1",
            TripInvitationPeriodPreview.FromDates(new[]
            {
                new DateOnly(2027, 7, 28),
                new DateOnly(2027, 8, 2),
            }),
            TripInvitationMemberCountBand.TwoToFive,
            CreatedAtUtc,
            CreatedAtUtc.AddDays(7));
    }
}
