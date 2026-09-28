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

    public HistoricalKeyYearFactReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
    }

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        BsonDocument[] stages = BuildPipeline(limit).ToArray();
        PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument> pipeline =
            PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument>.Create(stages);
        List<HistoricalFactDocument> documents = await this.collection
            .Aggregate(pipeline)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    internal static IReadOnlyCollection<BsonDocument> BuildPipeline(int limit)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$sort", new BsonDocument
            {
                ["factId"] = -1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$factId",
                ["latestRevision"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latestRevision")),
            new BsonDocument("$match", new BsonDocument
            {
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
            new BsonDocument("$limit", limit),
        };
    }
}
