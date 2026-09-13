using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ProfileComparisonInvitationMongoMapperTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapping_ShouldRoundTripPendingAndAcceptedConsentStates()
    {
        ProfileComparisonInvitation pending = CreatePending();
        ProfileComparisonInvitationDocument pendingDocument = pending.ToDocument();

        Assert.Equal(pending.ExpiresAtUtc.AddDays(7), pendingDocument.PurgeAtUtc);
        Assert.Equal(pending.Categories, pendingDocument.ToDomain().Categories);

        pending.Accept(
            "invitee-1",
            SharePublicationId.Parse("invitee-passport"),
            2,
            ProfileComparisonId.Parse("comparison-1"),
            NowUtc.AddMinutes(5));
        ProfileComparisonInvitationDocument acceptedDocument = pending.ToDocument();
        ProfileComparisonInvitation accepted = acceptedDocument.ToDomain();

        Assert.Null(acceptedDocument.PurgeAtUtc);
        Assert.True(accepted.IsAccepted);
        Assert.Equal("invitee-1", accepted.AcceptorUserId);
        Assert.Equal("comparison-1", accepted.ComparisonId!.Value.Value);
    }

    private static ProfileComparisonInvitation CreatePending()
    {
        return ProfileComparisonInvitation.Create(
            ProfileComparisonInvitationId.Parse("invitation-1"),
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            "creator-1",
            SharePublicationId.Parse("creator-passport"),
            1,
            new[] { ProfileComparisonCategory.VisitedParks },
            NowUtc,
            NowUtc.AddDays(7));
    }
}
