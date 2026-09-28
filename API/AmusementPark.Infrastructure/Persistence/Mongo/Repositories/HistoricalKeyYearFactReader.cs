using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalKeyYearFactReader : IHistoricalKeyYearFactReader
{
    private readonly IMongoCollection<HistoricalFactDocument> collection;
    private readonly string collectionName;

    public HistoricalKeyYearFactReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collectionName = settings.HistoricalFactsCollectionName;
        this.collection = database.GetCollection<HistoricalFactDocument>(
            this.collectionName);
    }

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParksAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        string[] normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkIds.Length == 0)
        {
            return Array.Empty<HistoricalFact>();
        }

        BsonDocument[] stages = BuildPipeline(normalizedParkIds, this.collectionName).ToArray();
        PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument> pipeline =
            PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument>.Create(stages);
        List<HistoricalFactDocument> documents = await this.collection
            .Aggregate(pipeline)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    internal static IReadOnlyCollection<BsonDocument> BuildPipeline(
        IReadOnlyCollection<string> parkIds,
        string collectionName)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        string normalizedCollectionName = collectionName?.Trim() ?? string.Empty;
        if (normalizedCollectionName.Length == 0)
        {
            throw new ArgumentException(
                "A historical facts collection name is required.",
                nameof(collectionName));
        }

        string[] normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkIds.Length == 0)
        {
            throw new ArgumentException("At least one park identifier is required.", nameof(parkIds));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument(
                "subject.contextParkId",
                new BsonDocument("$in", new BsonArray(normalizedParkIds)))),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$factId",
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                ["from"] = normalizedCollectionName,
                ["let"] = new BsonDocument("candidateFactId", "$_id"),
                ["pipeline"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument(
                        "$expr",
                        new BsonDocument("$eq", new BsonArray
                        {
                            "$factId",
                            "$$candidateFactId",
                        }))),
                    new BsonDocument("$sort", new BsonDocument("revision", -1)),
                    new BsonDocument("$limit", 1),
                },
                ["as"] = "latestRevision",
            }),
            new BsonDocument("$unwind", "$latestRevision"),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latestRevision")),
            new BsonDocument("$match", new BsonDocument
            {
                ["subject.contextParkId"] = new BsonDocument(
                    "$in",
                    new BsonArray(normalizedParkIds)),
                ["publicationState"] = HistoricalPublicationState.Published.ToString(),
                ["state"] = new BsonDocument("$in", new BsonArray
                {
                    HistoricalFactState.Verified.ToString(),
                    HistoricalFactState.Probable.ToString(),
                    HistoricalFactState.Disputed.ToString(),
                }),
            }),
            new BsonDocument("$sort", new BsonDocument
            {
                ["createdAt"] = -1,
                ["factId"] = 1,
            }),
        };
    }
}
