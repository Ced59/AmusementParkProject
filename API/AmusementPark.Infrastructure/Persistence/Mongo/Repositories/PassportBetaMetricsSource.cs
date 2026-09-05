using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportBetaMetricsSource : IPassportBetaMetricsSource
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<UserVisitDocument> visits;

    public PassportBetaMetricsSource(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal PassportBetaMetricsSource(IMongoCollection<UserVisitDocument> visits)
    {
        this.visits = visits ?? throw new ArgumentNullException(nameof(visits));
    }

    public async Task<PassportBetaMetricsSourceSnapshot> ReadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline = BuildPipeline(fromUtc, toUtc);
        List<BsonDocument> documents = await this.visits
            .Aggregate<BsonDocument>(
                pipeline,
                new AggregateOptions
                {
                    AllowDiskUse = true,
                    MaxTime = QueryTimeout,
                })
            .ToListAsync(cancellationToken);
        BsonDocument root = documents.SingleOrDefault() ?? new BsonDocument();
        return MapSnapshot(root, fromUtc, toUtc);
    }

    internal static BsonDocument[] BuildPipeline(DateTime fromUtc, DateTime toUtc)
    {
        return
        [
            new BsonDocument("$match", new BsonDocument("$and", new BsonArray
            {
                new BsonDocument(
                    UserVisitMongoDefinitions.DeletedAtUtcPath,
                    BsonNull.Value),
                new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument(
                        "createdAt",
                        new BsonDocument
                        {
                            ["$gte"] = fromUtc,
                            ["$lte"] = toUtc,
                        }),
                    new BsonDocument(
                        "completedAtUtc",
                        new BsonDocument
                        {
                            ["$ne"] = BsonNull.Value,
                            ["$lte"] = toUtc,
                        }),
                }),
            })),
            new BsonDocument("$facet", new BsonDocument
            {
                ["created"] = new BsonArray
                {
                    BuildDateMatchStage("createdAt", fromUtc, toUtc),
                    new BsonDocument("$count", "count"),
                },
                ["completed"] = new BsonArray
                {
                    BuildDateMatchStage("completedAtUtc", fromUtc, toUtc),
                    new BsonDocument("$count", "count"),
                },
                ["completedDaily"] = new BsonArray
                {
                    BuildDateMatchStage("completedAtUtc", fromUtc, toUtc),
                    BuildDailyGroupStage("$completedAtUtc"),
                    new BsonDocument("$sort", new BsonDocument("_id", 1)),
                },
                ["cohort"] = BuildCohortPipeline(fromUtc, toUtc),
            }),
        ];
    }

    internal static PassportBetaMetricsSourceSnapshot MapSnapshot(
        BsonDocument root,
        DateTime fromUtc,
        DateTime toUtc)
    {
        long createdVisits = ReadFacetCount(root, "created");
        long completedVisits = ReadFacetCount(root, "completed");
        BsonDocument cohort = ReadFirstDocument(root, "cohort");
        BsonDocument summary = ReadFirstDocument(cohort, "summary");
        long usersWithCompletedVisit = ReadInt64(summary, "usersWithCompletedVisit");
        long usersWithSecondCompletedVisit = ReadInt64(
            summary,
            "usersWithSecondCompletedVisit");
        IReadOnlyDictionary<string, long> completedByDate = ReadDailyCounts(
            root,
            "completedDaily");
        IReadOnlyDictionary<string, long> firstByDate = ReadDailyCounts(
            cohort,
            "firstVisitDaily");
        IReadOnlyDictionary<string, long> secondByDate = ReadDailyCounts(
            cohort,
            "secondVisitDaily");
        List<PassportBetaDailyMetrics> daily = BuildDailyMetrics(
            fromUtc,
            toUtc,
            completedByDate,
            firstByDate,
            secondByDate);

        return new PassportBetaMetricsSourceSnapshot(
            createdVisits,
            completedVisits,
            usersWithCompletedVisit,
            usersWithSecondCompletedVisit,
            daily);
    }

    private static BsonArray BuildCohortPipeline(DateTime fromUtc, DateTime toUtc)
    {
        return new BsonArray
        {
            new BsonDocument("$match", new BsonDocument(
                "completedAtUtc",
                new BsonDocument
                {
                    ["$ne"] = BsonNull.Value,
                    ["$lte"] = toUtc,
                })),
            new BsonDocument("$sort", new BsonDocument
            {
                ["userId"] = 1,
                ["completedAtUtc"] = 1,
                ["_id"] = 1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$userId",
                ["completionDates"] = new BsonDocument("$push", "$completedAtUtc"),
            }),
            new BsonDocument("$project", new BsonDocument
            {
                ["_id"] = 0,
                ["firstCompletedAt"] = new BsonDocument(
                    "$arrayElemAt",
                    new BsonArray { "$completionDates", 0 }),
                ["secondCompletedAt"] = new BsonDocument(
                    "$arrayElemAt",
                    new BsonArray { "$completionDates", 1 }),
            }),
            new BsonDocument("$facet", new BsonDocument
            {
                ["summary"] = new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        ["_id"] = BsonNull.Value,
                        ["usersWithCompletedVisit"] = new BsonDocument(
                            "$sum",
                            BuildMilestoneInRangeCounter(
                                "firstCompletedAt",
                                fromUtc,
                                toUtc)),
                        ["usersWithSecondCompletedVisit"] = new BsonDocument(
                            "$sum",
                            BuildMilestoneInRangeCounter(
                                "secondCompletedAt",
                                fromUtc,
                                toUtc)),
                    }),
                },
                ["firstVisitDaily"] = BuildMilestoneDailyPipeline(
                    "firstCompletedAt",
                    fromUtc,
                    toUtc),
                ["secondVisitDaily"] = BuildMilestoneDailyPipeline(
                    "secondCompletedAt",
                    fromUtc,
                    toUtc),
            }),
        };
    }

    private static BsonDocument BuildMilestoneInRangeCounter(
        string fieldName,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return new BsonDocument(
            "$cond",
            new BsonArray
            {
                new BsonDocument(
                    "$and",
                    new BsonArray
                    {
                        new BsonDocument(
                            "$gte",
                            new BsonArray { $"${fieldName}", fromUtc }),
                        new BsonDocument(
                            "$lte",
                            new BsonArray { $"${fieldName}", toUtc }),
                    }),
                1,
                0,
            });
    }

    private static BsonArray BuildMilestoneDailyPipeline(
        string fieldName,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return new BsonArray
        {
            BuildDateMatchStage(fieldName, fromUtc, toUtc),
            BuildDailyGroupStage($"${fieldName}"),
            new BsonDocument("$sort", new BsonDocument("_id", 1)),
        };
    }

    private static BsonDocument BuildDateMatchStage(
        string fieldName,
        DateTime fromUtc,
        DateTime toUtc)
    {
        return new BsonDocument("$match", new BsonDocument(
            fieldName,
            new BsonDocument
            {
                ["$gte"] = fromUtc,
                ["$lte"] = toUtc,
            }));
    }

    private static BsonDocument BuildDailyGroupStage(string dateExpression)
    {
        return new BsonDocument("$group", new BsonDocument
        {
            ["_id"] = new BsonDocument("$dateToString", new BsonDocument
            {
                ["format"] = "%Y-%m-%d",
                ["date"] = dateExpression,
                ["timezone"] = "UTC",
            }),
            ["count"] = new BsonDocument("$sum", 1),
        });
    }

    private static IReadOnlyDictionary<string, long> ReadDailyCounts(
        BsonDocument document,
        string elementName)
    {
        if (!document.TryGetValue(elementName, out BsonValue? value)
            || value is not BsonArray array)
        {
            return new Dictionary<string, long>(StringComparer.Ordinal);
        }

        return array
            .Where(static item => item.IsBsonDocument)
            .Select(static item => item.AsBsonDocument)
            .Where(static item => item.TryGetValue("_id", out BsonValue? id)
                && id.IsString)
            .ToDictionary(
                static item => item["_id"].AsString,
                static item => ReadInt64(item, "count"),
                StringComparer.Ordinal);
    }

    private static List<PassportBetaDailyMetrics> BuildDailyMetrics(
        DateTime fromUtc,
        DateTime toUtc,
        IReadOnlyDictionary<string, long> completedByDate,
        IReadOnlyDictionary<string, long> firstByDate,
        IReadOnlyDictionary<string, long> secondByDate)
    {
        List<PassportBetaDailyMetrics> result = new List<PassportBetaDailyMetrics>();
        for (DateTime dayUtc = fromUtc.Date;
            dayUtc <= toUtc.Date;
            dayUtc = dayUtc.AddDays(1))
        {
            string date = dayUtc.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            result.Add(new PassportBetaDailyMetrics(
                date,
                completedByDate.GetValueOrDefault(date),
                firstByDate.GetValueOrDefault(date),
                secondByDate.GetValueOrDefault(date)));
        }

        return result;
    }

    private static long ReadFacetCount(BsonDocument root, string elementName)
    {
        return ReadInt64(ReadFirstDocument(root, elementName), "count");
    }

    private static BsonDocument ReadFirstDocument(BsonDocument root, string elementName)
    {
        if (!root.TryGetValue(elementName, out BsonValue? value)
            || value is not BsonArray array
            || array.Count == 0
            || !array[0].IsBsonDocument)
        {
            return new BsonDocument();
        }

        return array[0].AsBsonDocument;
    }

    private static long ReadInt64(BsonDocument document, string elementName)
    {
        return document.TryGetValue(elementName, out BsonValue? value)
            && value.IsNumeric
                ? value.ToInt64()
                : 0L;
    }

    private static IMongoCollection<UserVisitDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);

        return database.GetCollection<UserVisitDocument>(settings.UserVisitsCollectionName);
    }
}
