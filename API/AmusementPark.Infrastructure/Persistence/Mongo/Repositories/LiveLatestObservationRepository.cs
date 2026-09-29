using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveLatestObservationRepository : ILiveLatestObservationRepository
{
    private const int MaximumTargetIdsPerQuery = 250;

    private readonly IMongoCollection<LiveLatestObservationDocument> collection;

    public LiveLatestObservationRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<LiveLatestObservationDocument>(
            settings.LiveLatestObservationsCollectionName);
    }

    public async Task<IReadOnlyCollection<LiveLatestObservation>> GetByTargetAsync(
        LiveTargetType targetType,
        string targetId,
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedTargetId = NormalizeIdentifier(targetId, nameof(targetId));
        string normalizedParkId = NormalizeIdentifier(parkId, nameof(parkId));
        FilterDefinition<LiveLatestObservationDocument> filter =
            Builders<LiveLatestObservationDocument>.Filter.And(
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.type",
                    targetType.ToString()),
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.id",
                    normalizedTargetId),
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.parkId",
                    normalizedParkId));
        List<LiveLatestObservationDocument> documents = await this.collection
            .Find(filter)
            .Limit(16)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<LiveLatestObservation>> GetParkItemsAsync(
        string parkId,
        IReadOnlyCollection<string> targetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetIds);
        string normalizedParkId = NormalizeIdentifier(parkId, nameof(parkId));
        List<string> normalizedTargetIds = targetIds
            .Select(targetId => NormalizeIdentifier(targetId, nameof(targetIds)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static targetId => targetId, StringComparer.Ordinal)
            .ToList();
        if (normalizedTargetIds.Count == 0)
        {
            return Array.Empty<LiveLatestObservation>();
        }

        List<LiveLatestObservationDocument> documents = new List<LiveLatestObservationDocument>();
        foreach (string[] targetIdBatch in normalizedTargetIds.Chunk(MaximumTargetIdsPerQuery))
        {
            FilterDefinition<LiveLatestObservationDocument> filter =
                LiveLatestObservationMongoDefinitions.BuildParkItemsReadFilter(
                    normalizedParkId,
                    targetIdBatch);
            List<LiveLatestObservationDocument> batch = await this.collection
                .Find(filter)
                .SortBy(static document => document.Target.Id)
                .ThenBy(static document => document.SourceId)
                .ToListAsync(cancellationToken);
            documents.AddRange(batch);
        }

        return documents.Select(static document => document.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<LiveLatestObservationWriteResult> WriteLatestAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return new LiveLatestObservationWriteResult(
                0,
                0,
                0,
                Array.Empty<LiveLatestObservation>());
        }

        List<LiveLatestObservationDocument> incomingDocuments = observations
            .Select(static observation => observation.ToDocument())
            .ToList();
        List<WriteModel<LiveLatestObservationDocument>> writes = incomingDocuments
            .Select(static document => new UpdateOneModel<LiveLatestObservationDocument>(
                LiveLatestObservationMongoDefinitions.BuildNaturalKeyFilter(document),
                LiveLatestObservationMongoDefinitions.BuildMonotonicUpdate(document))
            {
                IsUpsert = true,
            })
            .Cast<WriteModel<LiveLatestObservationDocument>>()
            .ToList();
        BulkWriteResult<LiveLatestObservationDocument> result = await this.collection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        int insertedCount = result.Upserts.Count;
        int updatedCount = checked((int)result.ModifiedCount);
        int ignoredCount = observations.Count - insertedCount - updatedCount;
        string[] naturalIds = incomingDocuments
            .Select(static document => document.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        List<LiveLatestObservationDocument> committedDocuments = await this.collection.Find(
            Builders<LiveLatestObservationDocument>.Filter.In(
                static document => document.Id,
                naturalIds))
            .ToListAsync(cancellationToken);
        LiveLatestObservation[] committedObservations = committedDocuments
            .Select(static document => document.ToDomain())
            .ToArray();
        return new LiveLatestObservationWriteResult(
            insertedCount,
            updatedCount,
            ignoredCount,
            committedObservations);
    }

    private static string NormalizeIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A live target identifier is required.", parameterName);
        }

        return value.Trim();
    }
}
