using AmusementPark.Application.Features.Passport.Models;
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

    [Fact]
    public void HashPayload_ShouldIgnoreDerivedHistoricalConsistencyOnRetry()
    {
        RideOccurrence verified = CreateOccurrence("occurrence-1", "item-1", 1024, NowUtc);
        RideOccurrence unverified = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-2"),
            Visit.Create(
                VisitId.Parse("visit-1"),
                "user-1",
                "park-1",
                VisitDate.ForDay(2026, 9, 3),
                "Europe/Paris",
                LocalServiceDayConvention.VisitStartLocalDate,
                null,
                null,
                NowUtc),
            "item-1",
            2048,
            verified.Moment,
            verified.Status,
            verified.Source,
            HistoricalConsistency.Unverified,
            null,
            verified.PrivateNote,
            NowUtc.AddMinutes(1));

        Assert.Equal(
            UserRideOccurrenceCreationFingerprint.HashPayload(new[] { verified }),
            UserRideOccurrenceCreationFingerprint.HashPayload(new[] { unverified }));
    }

    [Fact]
    public void HashReorderPayload_ShouldIncludeExpectedVersionAndPlacement()
    {
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            RideOccurrenceId.Parse("occurrence-1"),
            1,
            null,
            RideOccurrencePlacement.Last);

        string original = UserRideOccurrenceCreationFingerprint.HashReorderPayload(request);
        string changed = UserRideOccurrenceCreationFingerprint.HashReorderPayload(
            request with
            {
                ExpectedVersion = 2,
                Placement = RideOccurrencePlacement.First,
            });

        Assert.NotEqual(original, changed);
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
