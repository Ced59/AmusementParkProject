using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Core.Domain.SocialShare;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class SocialShareEventRepository : ISocialShareEventRepository
{
    private readonly IMongoCollection<SocialShareEventDocument> collection;

    public SocialShareEventRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<SocialShareEventDocument>(settings.SocialShareEventsCollectionName);
    }

    public async Task<SocialShareEvent> CreateAsync(SocialShareEvent shareEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shareEvent);

        DateTime occurredAtUtc = shareEvent.OccurredAtUtc == default ? DateTime.UtcNow : shareEvent.OccurredAtUtc;
        shareEvent.Id = string.IsNullOrWhiteSpace(shareEvent.Id) ? Guid.NewGuid().ToString("N") : shareEvent.Id;
        shareEvent.OccurredAtUtc = occurredAtUtc;
        shareEvent.CreatedAtUtc = occurredAtUtc;
        shareEvent.UpdatedAtUtc = occurredAtUtc;

        SocialShareEventDocument document = shareEvent.ToDocument();
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<SocialShareStatsResult> GetStatsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        FilterDefinition<SocialShareEventDocument> filter = Builders<SocialShareEventDocument>.Filter.Gte(static document => document.OccurredAtUtc, fromUtc)
            & Builders<SocialShareEventDocument>.Filter.Lte(static document => document.OccurredAtUtc, toUtc);

        Task<long> totalTask = this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        Task<long> anonymousTask = this.collection.CountDocumentsAsync(filter & Builders<SocialShareEventDocument>.Filter.Eq(static document => document.VisitorKind, SocialShareVisitorKind.Anonymous.ToString()), cancellationToken: cancellationToken);
        Task<long> authenticatedTask = this.collection.CountDocumentsAsync(filter & Builders<SocialShareEventDocument>.Filter.Eq(static document => document.VisitorKind, SocialShareVisitorKind.Authenticated.ToString()), cancellationToken: cancellationToken);
        Task<IReadOnlyCollection<SocialShareDailyStatsPoint>> dailyTask = this.GetDailyStatsAsync(fromUtc, toUtc, cancellationToken);
        Task<IReadOnlyCollection<SocialShareDimensionCount>> channelsTask = this.GetDimensionStatsAsync("channel", fromUtc, toUtc, cancellationToken);
        Task<IReadOnlyCollection<SocialShareDimensionCount>> targetTypesTask = this.GetDimensionStatsAsync("targetType", fromUtc, toUtc, cancellationToken);
        Task<IReadOnlyCollection<SocialShareDimensionCount>> visitorKindsTask = this.GetDimensionStatsAsync("visitorKind", fromUtc, toUtc, cancellationToken);
        Task<IReadOnlyCollection<SocialShareTopTarget>> topTargetsTask = this.GetTopTargetsAsync(fromUtc, toUtc, cancellationToken);

        await Task.WhenAll(totalTask, anonymousTask, authenticatedTask, dailyTask, channelsTask, targetTypesTask, visitorKindsTask, topTargetsTask);

        return new SocialShareStatsResult(
            fromUtc,
            toUtc,
            totalTask.Result,
            anonymousTask.Result,
            authenticatedTask.Result,
            dailyTask.Result,
            channelsTask.Result,
            targetTypesTask.Result,
            visitorKindsTask.Result,
            topTargetsTask.Result);
    }

    private async Task<IReadOnlyCollection<SocialShareDailyStatsPoint>> GetDailyStatsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        {
            BuildMatchStage(fromUtc, toUtc),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument("$dateToString", new BsonDocument
                {
                    ["format"] = "%Y-%m-%d",
                    ["date"] = "$occurredAtUtc",
                    ["timezone"] = "UTC",
                }),
                ["count"] = new BsonDocument("$sum", 1),
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1)),
        };

        List<BsonDocument> documents = await this.collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
        return documents
            .Select(static document => new SocialShareDailyStatsPoint(document["_id"].AsString, document["count"].ToInt64()))
            .ToList();
    }

    private async Task<IReadOnlyCollection<SocialShareDimensionCount>> GetDimensionStatsAsync(string fieldName, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        {
            BuildMatchStage(fromUtc, toUtc),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = $"${fieldName}",
                ["count"] = new BsonDocument("$sum", 1),
            }),
            new BsonDocument("$sort", new BsonDocument("count", -1)),
        };

        List<BsonDocument> documents = await this.collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
        return documents
            .Where(static document => !document["_id"].IsBsonNull)
            .Select(static document => new SocialShareDimensionCount(document["_id"].AsString, document["count"].ToInt64()))
            .ToList();
    }

    private async Task<IReadOnlyCollection<SocialShareTopTarget>> GetTopTargetsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline =
        {
            BuildMatchStage(fromUtc, toUtc),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = new BsonDocument
                {
                    ["targetType"] = "$targetType",
                    ["targetId"] = "$targetId",
                    ["targetTitle"] = "$targetTitle",
                    ["url"] = "$url",
                },
                ["count"] = new BsonDocument("$sum", 1),
            }),
            new BsonDocument("$sort", new BsonDocument("count", -1)),
            new BsonDocument("$limit", 10),
        };

        List<BsonDocument> documents = await this.collection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
        return documents.Select(ToTopTarget).ToList();
    }

    private static BsonDocument BuildMatchStage(DateTime fromUtc, DateTime toUtc)
    {
        return new BsonDocument("$match", new BsonDocument("occurredAtUtc", new BsonDocument
        {
            ["$gte"] = fromUtc,
            ["$lte"] = toUtc,
        }));
    }

    private static SocialShareTopTarget ToTopTarget(BsonDocument document)
    {
        BsonDocument key = document["_id"].AsBsonDocument;
        return new SocialShareTopTarget(
            ReadNullableString(key, "targetType") ?? SocialShareTargetType.Page.ToString(),
            ReadNullableString(key, "targetId"),
            ReadNullableString(key, "targetTitle"),
            ReadNullableString(key, "url") ?? string.Empty,
            document["count"].ToInt64());
    }

    private static string? ReadNullableString(BsonDocument document, string elementName)
    {
        if (!document.TryGetValue(elementName, out BsonValue value) || value.IsBsonNull)
        {
            return null;
        }

        return value.AsString;
    }
}
