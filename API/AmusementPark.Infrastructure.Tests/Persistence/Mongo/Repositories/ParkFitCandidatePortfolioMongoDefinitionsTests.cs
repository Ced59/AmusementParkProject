using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkFitCandidatePortfolioMongoDefinitionsTests
{
    [Fact]
    public void BuildPipeline_ShouldFilterPublicCandidatesBeforeJoiningOperationalStates()
    {
        BsonDocument[] pipeline = ParkFitCandidatePortfolioMongoDefinitions.BuildActivePipeline(
            "parks",
            " fr ",
            200);

        BsonDocument match = pipeline[3]["$match"].AsBsonDocument;
        BsonArray conditions = match["$and"].AsBsonArray;
        Assert.Contains(conditions, static value =>
            value.AsBsonDocument.TryGetValue("park.isVisible", out BsonValue? visibility)
            && visibility.IsBoolean
            && visibility.AsBoolean);
        Assert.Contains(conditions, static value =>
            value.AsBsonDocument.TryGetValue("park.countryCode", out BsonValue? country)
            && country == "FR");
        Assert.Contains(conditions, static value =>
            value.AsBsonDocument.TryGetValue("park.status", out BsonValue? status)
            && status == "Operating");

        BsonDocument lookup = pipeline[1]["$lookup"].AsBsonDocument;
        Assert.Equal("parks", lookup["from"].AsString);
        Assert.Equal("_id", lookup["localField"].AsString);
        Assert.Equal("_id", lookup["foreignField"].AsString);
    }

    [Fact]
    public void BuildPipeline_ShouldLimitOnlyTheAlreadyActiveCohort()
    {
        BsonDocument[] pipeline = ParkFitCandidatePortfolioMongoDefinitions.BuildActivePipeline(
            "parks",
            null,
            200);

        BsonArray operationalStates = pipeline[0]["$match"]["state"]["$in"].AsBsonArray;
        Assert.Equal(new[] { "Active", "Suspended" }, operationalStates.Select(
            static state => state.AsString));

        BsonDocument facets = pipeline[4]["$facet"].AsBsonDocument;
        BsonArray activeCandidates = facets["activeCandidates"].AsBsonArray;
        Assert.Equal(
            "Active",
            activeCandidates[0]["$match"]["state"]
                .AsString);
        Assert.Equal(200, activeCandidates[2]["$limit"].AsInt32);

        BsonArray stateCounts = facets["stateCounts"].AsBsonArray;
        Assert.Equal("$state", stateCounts[0]["$group"]["_id"].AsString);
    }

    [Fact]
    public void MapPortfolio_ShouldDeriveInactiveCountAndDetectActiveTruncation()
    {
        ParkDocument park = new ParkDocument
        {
            Id = "park-1",
            Name = "Parc actif",
            IsVisible = true,
        };
        BsonDocument root = new BsonDocument
        {
            ["activeCandidates"] = new BsonArray { park.ToBsonDocument() },
            ["stateCounts"] = new BsonArray
            {
                new BsonDocument
                {
                    ["_id"] = "Active",
                    ["count"] = 201L,
                },
                new BsonDocument
                {
                    ["_id"] = "Suspended",
                    ["count"] = 3L,
                },
            },
        };

        ParkFitCandidatePortfolio portfolio =
            ParkFitCandidatePortfolioReadRepository.MapPortfolio(root, 1_000L);

        Assert.Equal("park-1", Assert.Single(portfolio.ActiveCandidates).Id);
        Assert.Equal(1, portfolio.InspectedCandidateCount);
        Assert.Equal(3, portfolio.OperationallySuspendedCandidateCount);
        Assert.Equal(796, portfolio.NotActivatedCandidateCount);
        Assert.True(portfolio.CandidatePoolTruncated);
    }
}
