using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveOperationalControlRepository : ILiveOperationalControlRepository
{
    private readonly IMongoCollection<LiveOperationalControlDocument> collection;

    public LiveOperationalControlRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<LiveOperationalControlDocument>(
            settings.LiveOperationalControlsCollectionName);
    }

    public async Task<LiveOperationalControl?> GetLatestAsync(
        LiveOperationalControlScope scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        LiveOperationalControlDocument? document = await this.collection
            .Find(BuildScopeFilter(scope))
            .SortByDescending(static item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<LiveOperationalControl>> GetLatestBySourceAsync(
        LiveDataSourceId sourceId,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> stages = new List<BsonDocument>
        {
            new BsonDocument("$match", new BsonDocument("sourceId", sourceId.Value)),
            new BsonDocument("$sort", new BsonDocument
            {
                ["controlId"] = 1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$controlId",
                ["document"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$document")),
            new BsonDocument("$sort", new BsonDocument
            {
                ["scopeType"] = 1,
                ["internalParkId"] = 1,
                ["internalTargetId"] = 1,
            }),
        };
        PipelineDefinition<LiveOperationalControlDocument, BsonDocument> pipeline =
            PipelineDefinition<LiveOperationalControlDocument, BsonDocument>.Create(stages);
        List<BsonDocument> documents = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents
            .Select(static document => BsonSerializer.Deserialize<LiveOperationalControlDocument>(document))
            .Select(static document => document.ToDomain())
            .ToArray();
    }

    public async Task<LiveOperationalControlWriteOutcome> AppendRevisionAsync(
        LiveOperationalControl control,
        int expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (expectedRevision < 0 || control.Revision != expectedRevision + 1)
        {
            return LiveOperationalControlWriteOutcome.Conflict;
        }

        LiveOperationalControl? current = await this.GetLatestAsync(control.Scope, cancellationToken);
        if ((expectedRevision == 0 && current is not null)
            || (expectedRevision > 0 && (current is null
                || current.Id != control.Id
                || current.Revision != expectedRevision)))
        {
            return LiveOperationalControlWriteOutcome.Conflict;
        }

        try
        {
            await this.collection.InsertOneAsync(
                control.ToDocument(),
                cancellationToken: cancellationToken);
            return LiveOperationalControlWriteOutcome.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return LiveOperationalControlWriteOutcome.Conflict;
        }
    }

    private static FilterDefinition<LiveOperationalControlDocument> BuildScopeFilter(
        LiveOperationalControlScope scope)
    {
        FilterDefinitionBuilder<LiveOperationalControlDocument> filters =
            Builders<LiveOperationalControlDocument>.Filter;
        return filters.Eq(static document => document.ScopeType, scope.Type)
            & filters.Eq(static document => document.SourceId, scope.SourceId.Value)
            & filters.Eq(static document => document.ExternalEntityId, scope.ExternalEntityId)
            & filters.Eq(static document => document.InternalParkId, scope.InternalParkId)
            & filters.Eq(static document => document.TargetType, scope.TargetType)
            & filters.Eq(static document => document.InternalTargetId, scope.InternalTargetId);
    }
}
