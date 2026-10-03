using AmusementPark.Application.Features.History.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalIdentityTests
{
    [Fact]
    public void CreateGuid_WhenTimestampOnlyDiffersBelowMongoPrecision_ReturnsSameIdentity()
    {
        DateTime persistedTimestamp = new DateTime(
            638950320001230000,
            DateTimeKind.Utc);
        DateTime inMemoryTimestamp = persistedTimestamp.AddTicks(4567);

        Guid persistedIdentity = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            "hist-canonical-v2",
            "fact",
            "history-1",
            persistedTimestamp);
        Guid inMemoryIdentity = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            "hist-canonical-v2",
            "fact",
            "history-1",
            inMemoryTimestamp);

        Assert.Equal(persistedIdentity, inMemoryIdentity);
    }

    [Fact]
    public void CreateGuid_WhenPersistedMillisecondChanges_ReturnsDifferentIdentity()
    {
        DateTime timestamp = new DateTime(
            638950320001230000,
            DateTimeKind.Utc);

        Guid first = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            "hist-canonical-v2",
            "fact",
            "history-1",
            timestamp);
        Guid second = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            "hist-canonical-v2",
            "fact",
            "history-1",
            timestamp.AddMilliseconds(1));

        Assert.NotEqual(first, second);
    }
}
