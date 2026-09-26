using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class HistoricalFactPublicProjectionMigrationTests
{
    [Fact]
    public void ResolveContextParkId_WhenCurrentTargetWasDeleted_ShouldUseRetainedNarrativeScope()
    {
        HistoricalSubjectDocument subject = new HistoricalSubjectDocument
        {
            Type = HistoricalSubjectType.ParkItem,
            Id = "deleted-item",
            HistoricalLabel = "Attraction disparue",
            PublicationPolicy = HistoricalSubjectPublicationPolicy.HistoricalOnly,
        };

        string? parkId = HistoricalFactPublicProjectionMigration.ResolveContextParkId(
            subject,
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>
            {
                ["deleted-item"] = "park-1",
            });

        Assert.Equal("park-1", parkId);
    }

    [Fact]
    public void ResolveContextParkId_WhenFactAlreadyCarriesScope_ShouldKeepIt()
    {
        HistoricalSubjectDocument subject = new HistoricalSubjectDocument
        {
            Type = HistoricalSubjectType.ParkItem,
            Id = "item-1",
            HistoricalLabel = "Attraction",
            PublicationPolicy = HistoricalSubjectPublicationPolicy.HistoricalOnly,
            ContextParkId = "park-frozen",
        };

        string? parkId = HistoricalFactPublicProjectionMigration.ResolveContextParkId(
            subject,
            new Dictionary<string, string>
            {
                ["item-1"] = "park-current",
            },
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        Assert.Equal("park-frozen", parkId);
    }
}
