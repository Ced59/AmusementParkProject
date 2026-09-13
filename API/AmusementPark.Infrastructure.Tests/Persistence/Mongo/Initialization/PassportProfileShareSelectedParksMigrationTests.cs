using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class PassportProfileShareSelectedParksMigrationTests
{
    [Fact]
    public void BuildSelectedParks_ShouldFreezeEverySelectionWithoutLeakingIdsAsLabels()
    {
        IReadOnlyDictionary<string, ParkDocument> currentParks =
            new Dictionary<string, ParkDocument>(StringComparer.Ordinal)
            {
                ["park-a"] = new ParkDocument
                {
                    Id = "park-a",
                    Name = "Nom actuel A",
                    CountryCode = "fr",
                },
                ["park-b"] = new ParkDocument
                {
                    Id = "park-b",
                    Name = "Parc B",
                    CountryCode = "be",
                },
            };
        PassportProfileShareParkDocument[] frozenParks =
        {
            new PassportProfileShareParkDocument
            {
                Name = "Ancien nom A",
                CountryCode = "FR",
            },
        };

        IReadOnlyCollection<PassportProfileShareSelectedParkDocument> result =
            PassportProfileShareSelectedParksMigration.BuildSelectedParks(
                new[] { "park-a", "park-b", "park-deleted" },
                frozenParks,
                currentParks);

        PassportProfileShareSelectedParkDocument[] parks = result.ToArray();
        Assert.Equal(new[] { "park-a", "park-b", "park-deleted" }, parks.Select(
            static park => park.ParkId));
        Assert.Equal("Ancien nom A", parks[0].Name);
        Assert.Equal("FR", parks[0].CountryCode);
        Assert.Equal("Parc B", parks[1].Name);
        Assert.Equal("BE", parks[1].CountryCode);
        Assert.Equal("Unavailable park", parks[2].Name);
        Assert.Null(parks[2].CountryCode);
        Assert.DoesNotContain(parks, static park => string.Equals(
            park.Name,
            park.ParkId,
            StringComparison.Ordinal));
    }

    [Fact]
    public void BuildSelectedParks_ShouldMatchFrozenLabelsBeforeDeterministicFallback()
    {
        IReadOnlyDictionary<string, ParkDocument> currentParks =
            new Dictionary<string, ParkDocument>(StringComparer.Ordinal)
            {
                ["park-a"] = new ParkDocument
                {
                    Id = "park-a",
                    Name = "Parc A",
                    CountryCode = "FR",
                },
                ["park-b"] = new ParkDocument
                {
                    Id = "park-b",
                    Name = "Parc B",
                    CountryCode = "DE",
                },
            };
        PassportProfileShareParkDocument[] frozenParks =
        {
            new PassportProfileShareParkDocument { Name = "Parc B", CountryCode = "DE" },
            new PassportProfileShareParkDocument { Name = "Ancien nom A", CountryCode = "FR" },
        };

        PassportProfileShareSelectedParkDocument[] result =
            PassportProfileShareSelectedParksMigration.BuildSelectedParks(
                    new[] { "park-a", "park-b" },
                    frozenParks,
                    currentParks)
                .ToArray();

        Assert.Equal("Ancien nom A", result[0].Name);
        Assert.Equal("Parc B", result[1].Name);
    }
}
