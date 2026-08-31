using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RankingScopeRegistryTests
{
    private static readonly string[] ExpectedKeys =
    {
        "park-items:category:animal",
        "park-items:category:attraction",
        "park-items:category:hotel",
        "park-items:category:restaurant",
        "park-items:category:service",
        "park-items:category:shop",
        "park-items:category:show",
        "park-items:category:transport",
        "parks:global",
    };

    [Fact]
    public void CanonicalDefinitions_ShouldContainOnlyTheInitialPublishedScopes()
    {
        Assert.Equal(CanonicalRankingScopes.Version, CreateRegistry().Version);
        Assert.Equal(ExpectedKeys, CanonicalRankingScopes.All
            .Select(static definition => definition.Key.Value)
            .OrderBy(static key => key, StringComparer.Ordinal));
        Assert.All(CanonicalRankingScopes.All, static definition =>
        {
            Assert.True(definition.IsPublic);
            Assert.Equal(
                RankingEligibilityPolicy.InitialMethodologyVersion,
                definition.MethodologyVersion);
            Assert.Equal(
                RankingEligibilityPolicy.Initial.MinimumEligibleEntriesPerRanking,
                definition.MinimumEligibleEntries);
            Assert.Equal(RankingPublicationMode.DurableSnapshot, definition.PublicationMode);
        });
        Assert.DoesNotContain(
            CanonicalRankingScopes.All,
            static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Other);
    }

    [Fact]
    public void TryResolve_WhenScopeAndMethodologyAreKnown_ShouldReturnTheTypedDefinition()
    {
        IRankingScopeRegistry registry = CreateRegistry();

        bool found = registry.TryResolve(
            "park-items:category:attraction",
            RankingEligibilityPolicy.InitialMethodologyVersion,
            out RankingScopeDefinition? definition);

        Assert.True(found);
        Assert.NotNull(definition);
        Assert.Equal(RankingTargetFamily.ParkItems, definition.TargetFamily);
        Assert.Equal(ParkItemCategory.Attraction, definition.Filter.ParkItemCategory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" parks:global")]
    [InlineData("PARKS:GLOBAL")]
    [InlineData("parks:country:fr")]
    [InlineData("park-items:type:coaster")]
    [InlineData("park-items:category:other")]
    [InlineData("park-items:category:attraction?country=fr")]
    [InlineData("users:123:parks")]
    public void TryResolve_WhenScopeIsMalformedOrNotPublished_ShouldRejectIt(string? scopeKey)
    {
        IRankingScopeRegistry registry = CreateRegistry();

        bool found = registry.TryResolve(
            scopeKey,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            out RankingScopeDefinition? definition);

        Assert.False(found);
        Assert.Null(definition);
    }

    [Fact]
    public void TryResolve_WhenMethodologyIsNotSupported_ShouldRejectIt()
    {
        IRankingScopeRegistry registry = CreateRegistry();

        bool found = registry.TryResolve(
            "parks:global",
            RatingMethodologyVersion.Parse("ratings-2026-02"),
            out RankingScopeDefinition? definition);

        Assert.False(found);
        Assert.Null(definition);
    }

    [Fact]
    public void TryResolve_WhenMethodologyIsUninitialized_ShouldFailSafely()
    {
        IRankingScopeRegistry registry = CreateRegistry();

        bool found = registry.TryResolve(
            "parks:global",
            default,
            out RankingScopeDefinition? definition);

        Assert.False(found);
        Assert.Null(definition);
    }

    [Fact]
    public void Constructor_WhenDefinitionsContainTheSameKeyTwice_ShouldRejectThem()
    {
        RankingScopeDefinition definition = CanonicalRankingScopes.GlobalParks;

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new RankingScopeRegistry(
            "ranking-scopes-test",
            new[] { definition, definition }));

        Assert.Contains("parks:global", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Definitions_ShouldNotAllowRuntimeMutation()
    {
        IRankingScopeRegistry registry = CreateRegistry();
        ICollection<RankingScopeDefinition> definitions =
            Assert.IsAssignableFrom<ICollection<RankingScopeDefinition>>(registry.Definitions);

        Assert.True(definitions.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => definitions.Add(CanonicalRankingScopes.GlobalParks));
    }

    private static RankingScopeRegistry CreateRegistry()
    {
        return new RankingScopeRegistry(CanonicalRankingScopes.Version, CanonicalRankingScopes.All);
    }
}
