using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class TripInvitationMongoMapperTests
{
    [Fact]
    public void ToDocument_ShouldRetainTheIdempotencyTombstoneAfterLinkExpiration()
    {
        DateTime createdAtUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime expiresAtUtc = createdAtUtc.AddDays(7);
        TripInvitation invitation = TripInvitation.Create(
            TripInvitationId.Parse("invitation-1"),
            TripPlanId.Parse("trip-1"),
            "Voyage été",
            "token-hash",
            "hint42",
            TripDelegatedRole.Participant,
            TripMemberId.Parse("member-1"),
            "Camille",
            null,
            null,
            TripInvitationPeriodPreview.Unspecified(),
            TripInvitationMemberCountBand.One,
            createdAtUtc,
            expiresAtUtc);

        TripInvitationDocument document = invitation.ToDocument();

        Assert.Equal(
            expiresAtUtc.Add(TripInvitation.IdempotencyReplayRetention),
            document.RetentionExpiresAtUtc);
    }
}
