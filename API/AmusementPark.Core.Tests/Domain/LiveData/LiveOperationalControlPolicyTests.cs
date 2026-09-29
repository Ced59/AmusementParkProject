using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveOperationalControlPolicyTests
{
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("source");
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AllowsCollection_WhenSourceIsStopped_ShouldStopEveryDescendant()
    {
        LiveOperationalControl sourceControl = CreateControl(
            new LiveOperationalControlScope(
                LiveOperationalScopeType.Source,
                SourceId,
                null,
                null,
                null,
                null),
            false,
            true);
        LiveOperationalControlPolicy policy = new LiveOperationalControlPolicy();

        bool allowed = policy.AllowsCollection(
            true,
            new[] { sourceControl },
            "external-park",
            "park-1",
            LiveTargetType.ParkItem,
            "item-1");

        Assert.False(allowed);
    }

    [Fact]
    public void AllowsPublicRead_WhenTargetIsHidden_ShouldKeepSiblingVisible()
    {
        LiveOperationalControl targetControl = CreateControl(
            new LiveOperationalControlScope(
                LiveOperationalScopeType.Target,
                SourceId,
                "external-park",
                "park-1",
                LiveTargetType.ParkItem,
                "item-1"),
            true,
            false);
        LiveOperationalControlPolicy policy = new LiveOperationalControlPolicy();

        Assert.False(policy.AllowsPublicRead(
            true,
            new[] { targetControl },
            "external-park",
            "park-1",
            LiveTargetType.ParkItem,
            "item-1"));
        Assert.True(policy.AllowsPublicRead(
            true,
            new[] { targetControl },
            "external-park",
            "park-1",
            LiveTargetType.ParkItem,
            "item-2"));
    }

    [Fact]
    public void AllowsCollection_WhenConfigurationIsDisabled_ShouldIgnorePersistedEnablement()
    {
        LiveOperationalControl sourceControl = CreateControl(
            new LiveOperationalControlScope(
                LiveOperationalScopeType.Source,
                SourceId,
                null,
                null,
                null,
                null),
            true,
            true);

        bool allowed = new LiveOperationalControlPolicy().AllowsCollection(
            false,
            new[] { sourceControl },
            "external-park");

        Assert.False(allowed);
    }

    [Fact]
    public void Revise_ShouldPreserveIdentityAndAppendRevision()
    {
        LiveOperationalControl current = CreateControl(
            new LiveOperationalControlScope(
                LiveOperationalScopeType.Source,
                SourceId,
                null,
                null,
                null,
                null),
            true,
            true);

        LiveOperationalControl revised = current.Revise(
            false,
            false,
            "admin-2",
            "Provider incident",
            NowUtc.AddMinutes(1));

        Assert.Equal(current.Id, revised.Id);
        Assert.Equal(2, revised.Revision);
        Assert.Equal(1, revised.SupersedesRevision);
        Assert.False(revised.CollectionEnabled);
        Assert.False(revised.PublicReadEnabled);
    }

    private static LiveOperationalControl CreateControl(
        LiveOperationalControlScope scope,
        bool collectionEnabled,
        bool publicReadEnabled)
    {
        return new LiveOperationalControl(
            Guid.NewGuid(),
            scope,
            collectionEnabled,
            publicReadEnabled,
            1,
            null,
            "admin-1",
            "Operational decision",
            NowUtc);
    }
}
