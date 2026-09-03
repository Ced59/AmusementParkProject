using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceCreationFingerprintTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void HashPayload_ShouldIgnoreGeneratedIdentityPositionAndTimestamp()
    {
        RideOccurrence first = CreateOccurrence("occurrence-1", "item-1", 1024, NowUtc);
        RideOccurrence retry = CreateOccurrence(
            "occurrence-new",
            "item-1",
            4096,
            NowUtc.AddMinutes(1));

        string firstHash = UserRideOccurrenceCreationFingerprint.HashPayload(new[] { first });
        string retryHash = UserRideOccurrenceCreationFingerprint.HashPayload(new[] { retry });

        Assert.Equal(firstHash, retryHash);
        Assert.Equal(64, firstHash.Length);
    }

    [Fact]
    public void HashPayload_ShouldRemainOrderAndContentSensitive()
    {
        RideOccurrence first = CreateOccurrence("occurrence-1", "item-1", 1024, NowUtc);
        RideOccurrence second = CreateOccurrence("occurrence-2", "item-2", 2048, NowUtc);

        string ordered = UserRideOccurrenceCreationFingerprint.HashPayload(
            new[] { first, second });
        string reversed = UserRideOccurrenceCreationFingerprint.HashPayload(
            new[] { second, first });

        Assert.NotEqual(ordered, reversed);
    }

    [Fact]
    public void HashOperationKey_ShouldNotPersistTheRawClientValue()
    {
        string hash = UserRideOccurrenceCreationFingerprint.HashOperationKey("secret-operation");

        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain("secret-operation", hash, StringComparison.Ordinal);
    }

    private static RideOccurrence CreateOccurrence(
        string id,
        string parkItemId,
        long sortPosition,
        DateTime nowUtc)
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            nowUtc);
        return RideOccurrence.Create(
            RideOccurrenceId.Parse(id),
            visit,
            parkItemId,
            sortPosition,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
    }
}
