using System.Text;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPlanCreationFingerprintTests
{
    [Fact]
    public void HashPayload_ShouldIgnoreGeneratedIdentityAndTimestamps()
    {
        TripPlanCreationFingerprint fingerprint = CreateFingerprint("server-key-a-with-sufficient-entropy");
        TripPlan first = CreateTrip(
            "trip-1",
            "Voyage privé",
            new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc));
        TripPlan retry = CreateTrip(
            "trip-2",
            "Voyage privé",
            new DateTime(2026, 9, 17, 8, 5, 0, DateTimeKind.Utc));

        string firstHash = fingerprint.HashPayload(first);
        string retryHash = fingerprint.HashPayload(retry);

        Assert.Equal(firstHash, retryHash);
    }

    [Fact]
    public void HashPayload_ShouldChangeWhenRequestedContentChanges()
    {
        TripPlanCreationFingerprint fingerprint = CreateFingerprint("server-key-a-with-sufficient-entropy");
        DateTime nowUtc = new(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);
        TripPlan first = CreateTrip("trip-1", "Premier voyage", nowUtc);
        TripPlan changed = CreateTrip("trip-2", "Voyage modifié", nowUtc);

        string firstHash = fingerprint.HashPayload(first);
        string changedHash = fingerprint.HashPayload(changed);

        Assert.NotEqual(firstHash, changedHash);
    }

    [Fact]
    public void HashPayload_ShouldChangeWithFingerprintKey()
    {
        TripPlan tripPlan = CreateTrip(
            "trip-1",
            "Voyage privé",
            new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc));
        TripPlanCreationFingerprint first = CreateFingerprint("server-key-a-with-sufficient-entropy");
        TripPlanCreationFingerprint second = CreateFingerprint("server-key-b-with-sufficient-entropy");

        string firstHash = first.HashPayload(tripPlan);
        string secondHash = second.HashPayload(tripPlan);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void HashOwnerScope_ShouldBeStableAndBoundToTheFingerprintKey()
    {
        TripPlanCreationFingerprint first = CreateFingerprint("server-key-a-with-sufficient-entropy");
        TripPlanCreationFingerprint second = CreateFingerprint("server-key-b-with-sufficient-entropy");

        string firstHash = first.HashOwnerScope("user-1");

        Assert.Equal(firstHash, first.HashOwnerScope("user-1"));
        Assert.NotEqual(firstHash, second.HashOwnerScope("user-1"));
        Assert.DoesNotContain("user-1", firstHash, StringComparison.Ordinal);
    }

    [Fact]
    public void Fingerprints_ShouldRemainVerifiableAfterKeyRotation()
    {
        string firstKey = ToBase64("server-key-a-with-sufficient-entropy");
        string secondKey = ToBase64("server-key-b-with-sufficient-entropy");
        TripFingerprintKeyRingSettings initialSettings = new()
        {
            CurrentVersion = "v1",
            CurrentKey = firstKey,
        };
        TripFingerprintKeyRingSettings rotatedSettings = new()
        {
            CurrentVersion = "v2",
            CurrentKey = secondKey,
            PreviousKeys = $"v1={firstKey}",
        };
        TripPlanCreationFingerprint initial = new(initialSettings);
        TripPlanCreationFingerprint rotated = new(rotatedSettings);
        TripPlan tripPlan = CreateTrip(
            "trip-1",
            "Voyage privé",
            new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc));

        string originalPayloadHash = initial.HashPayload(tripPlan);
        string originalOwnerScope = initial.HashOwnerScope("user-1");

        Assert.Equal(originalPayloadHash, rotated.HashPayload(tripPlan, "v1"));
        Assert.Contains(originalOwnerScope, rotated.HashOwnerScopes("user-1"));
        Assert.Equal("v2", rotated.CurrentKeyVersion);
    }

    [Fact]
    public void Constructor_ShouldRejectMissingFingerprintKey()
    {
        TripFingerprintKeyRingSettings settings = new()
        {
            CurrentVersion = "v1",
            CurrentKey = " ",
        };

        Assert.Throws<InvalidOperationException>(() => new TripPlanCreationFingerprint(settings));
    }

    private static TripPlanCreationFingerprint CreateFingerprint(string signingKey)
    {
        TripFingerprintKeyRingSettings settings = new()
        {
            CurrentVersion = "v1",
            CurrentKey = ToBase64(signingKey),
        };
        return new TripPlanCreationFingerprint(settings);
    }

    private static string ToBase64(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static TripPlan CreateTrip(string id, string title, DateTime nowUtc)
    {
        return TripPlan.Create(
            TripPlanId.Parse(id),
            "user-1",
            title,
            TripDateProposal.Range(new DateOnly(2027, 6, 10), new DateOnly(2027, 6, 12)),
            "Europe/Paris",
            nowUtc);
    }
}
