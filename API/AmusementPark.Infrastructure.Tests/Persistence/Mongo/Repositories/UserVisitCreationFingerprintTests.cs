using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserVisitCreationFingerprintTests
{
    [Fact]
    public void HashPayload_ShouldIgnoreGeneratedIdentityAndTimestamps()
    {
        Visit first = CreateVisit(
            "visit-1",
            new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc),
            "Note privée");
        Visit retry = CreateVisit(
            "visit-2",
            new DateTime(2026, 9, 3, 8, 5, 0, DateTimeKind.Utc),
            "Note privée");

        string firstHash = UserVisitCreationFingerprint.HashPayload(first);
        string retryHash = UserVisitCreationFingerprint.HashPayload(retry);

        Assert.Equal(firstHash, retryHash);
    }

    [Fact]
    public void HashPayload_ShouldChangeWhenTheRequestedContentChanges()
    {
        DateTime nowUtc = new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);
        Visit first = CreateVisit("visit-1", nowUtc, "Première note");
        Visit changed = CreateVisit("visit-2", nowUtc, "Note modifiée");

        string firstHash = UserVisitCreationFingerprint.HashPayload(first);
        string changedHash = UserVisitCreationFingerprint.HashPayload(changed);

        Assert.NotEqual(firstHash, changedHash);
    }

    private static Visit CreateVisit(string visitId, DateTime nowUtc, string privateNote)
    {
        return Visit.Create(
            VisitId.Parse(visitId),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Journée d'été",
            privateNote,
            nowUtc);
    }
}
