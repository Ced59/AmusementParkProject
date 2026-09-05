using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportBetaMetricsSourceTests
{
    private static readonly DateTime FromUtc =
        new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ToUtc =
        new DateTime(2026, 9, 5, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public void BuildPipeline_ShouldAggregateUserCohortsBeforeReturningAnyResult()
    {
        BsonDocument[] pipeline = PassportBetaMetricsSource.BuildPipeline(FromUtc, ToUtc);

        BsonArray rootConditions = pipeline[0]["$match"]["$and"].AsBsonArray;
        Assert.Equal(BsonNull.Value, rootConditions[0]["deletedAtUtc"]);
        Assert.Equal(2, rootConditions[1]["$or"].AsBsonArray.Count);
        Assert.Equal(FromUtc, rootConditions[1]["$or"][0]["createdAt"]["$gte"].ToUniversalTime());
        Assert.Equal(ToUtc, rootConditions[1]["$or"][1]["completedAtUtc"]["$lte"].ToUniversalTime());
        BsonDocument facets = pipeline[1]["$facet"].AsBsonDocument;
        Assert.True(facets.Contains("created"));
        Assert.True(facets.Contains("completed"));
        Assert.True(facets.Contains("completedDaily"));
        BsonArray cohort = facets["cohort"].AsBsonArray;
        Assert.Contains(
            cohort,
            static stage => stage.IsBsonDocument
                && stage.AsBsonDocument.TryGetValue("$group", out BsonValue? group)
                && group["_id"] == "$userId");
        Assert.Equal(0, cohort[3]["$project"]["_id"].AsInt32);
        Assert.False(cohort[3]["$project"].AsBsonDocument.Contains("userId"));
        BsonDocument summaryGroup = cohort[4]["$facet"]["summary"][0]["$group"].AsBsonDocument;
        AssertMilestoneRangeCounter(
            summaryGroup["usersWithCompletedVisit"],
            "firstCompletedAt");
        AssertMilestoneRangeCounter(
            summaryGroup["usersWithSecondCompletedVisit"],
            "secondCompletedAt");
    }

    [Fact]
    public void MapSnapshot_ShouldCombineDailyMilestonesAndFillMissingDays()
    {
        BsonDocument root = new BsonDocument
        {
            ["created"] = new BsonArray { new BsonDocument("count", 7L) },
            ["completed"] = new BsonArray { new BsonDocument("count", 5L) },
            ["completedDaily"] = new BsonArray
            {
                Daily("2026-09-03", 2),
                Daily("2026-09-05", 3),
            },
            ["cohort"] = new BsonArray
            {
                new BsonDocument
                {
                    ["summary"] = new BsonArray
                    {
                        new BsonDocument
                        {
                            ["usersWithCompletedVisit"] = 4L,
                            ["usersWithSecondCompletedVisit"] = 2L,
                        },
                    },
                    ["firstVisitDaily"] = new BsonArray { Daily("2026-09-03", 1) },
                    ["secondVisitDaily"] = new BsonArray { Daily("2026-09-05", 2) },
                },
            },
        };

        PassportBetaMetricsSourceSnapshot result = PassportBetaMetricsSource.MapSnapshot(
            root,
            FromUtc,
            ToUtc);

        Assert.Equal(7, result.CreatedVisits);
        Assert.Equal(5, result.CompletedVisits);
        Assert.Equal(4, result.UsersWithCompletedVisit);
        Assert.Equal(2, result.UsersWithSecondCompletedVisit);
        Assert.Collection(
            result.Daily,
            first => Assert.Equal(
                new PassportBetaDailyMetrics("2026-09-03", 2, 1, 0),
                first),
            middle => Assert.Equal(
                new PassportBetaDailyMetrics("2026-09-04", 0, 0, 0),
                middle),
            last => Assert.Equal(
                new PassportBetaDailyMetrics("2026-09-05", 3, 0, 2),
                last));
    }

    [Fact]
    public void MapSnapshot_WithNoData_ShouldReturnZerosWithoutIdentifiers()
    {
        PassportBetaMetricsSourceSnapshot result = PassportBetaMetricsSource.MapSnapshot(
            new BsonDocument(),
            FromUtc,
            FromUtc);

        Assert.Equal(0, result.UsersWithCompletedVisit);
        Assert.Equal(0, result.UsersWithSecondCompletedVisit);
        Assert.Equal(new PassportBetaDailyMetrics("2026-09-03", 0, 0, 0), result.Daily.Single());
        Assert.Null(typeof(PassportBetaMetricsSourceSnapshot).GetProperty("UserId"));
    }

    private static BsonDocument Daily(string date, long count)
    {
        return new BsonDocument
        {
            ["_id"] = date,
            ["count"] = count,
        };
    }

    private static void AssertMilestoneRangeCounter(
        BsonValue counter,
        string fieldName)
    {
        BsonArray conditions = counter["$sum"]["$cond"][0]["$and"].AsBsonArray;
        Assert.Equal($"${fieldName}", conditions[0]["$gte"][0].AsString);
        Assert.Equal(FromUtc, conditions[0]["$gte"][1].ToUniversalTime());
        Assert.Equal($"${fieldName}", conditions[1]["$lte"][0].AsString);
        Assert.Equal(ToUtc, conditions[1]["$lte"][1].ToUniversalTime());
        Assert.Equal(1, counter["$sum"]["$cond"][1].AsInt32);
        Assert.Equal(0, counter["$sum"]["$cond"][2].AsInt32);
    }
}
