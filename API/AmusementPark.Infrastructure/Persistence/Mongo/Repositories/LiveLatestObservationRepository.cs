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
    private readonly IMongoCollection<LiveLatestObservationDocument> collection;

    public LiveLatestObservationRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<LiveLatestObservationDocument>(
            settings.LiveLatestObservationsCollectionName);
    }

    public async Task<IReadOnlyCollection<LiveLatestObservation>> GetByTargetAsync(
        LiveTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        string normalizedTargetId = NormalizeIdentifier(targetId, nameof(targetId));
        FilterDefinition<LiveLatestObservationDocument> filter =
            Builders<LiveLatestObservationDocument>.Filter.And(
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.type",
                    targetType.ToString()),
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.id",
                    normalizedTargetId));
        List<LiveLatestObservationDocument> documents = await this.collection
            .Find(filter)
            .Limit(16)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<LiveLatestObservation>> GetParkItemsAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = NormalizeIdentifier(parkId, nameof(parkId));
        FilterDefinition<LiveLatestObservationDocument> filter =
            Builders<LiveLatestObservationDocument>.Filter.And(
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.type",
                    LiveTargetType.ParkItem.ToString()),
                Builders<LiveLatestObservationDocument>.Filter.Eq(
                    "target.parkId",
                    normalizedParkId));
        List<LiveLatestObservationDocument> documents = await this.collection
            .Find(filter)
            .Limit(2_000)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList().AsReadOnly();
    }

    public async Task<LiveLatestObservationWriteResult> WriteLatestAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return new LiveLatestObservationWriteResult(0, 0, 0);
        }

        List<WriteModel<LiveLatestObservationDocument>> writes = observations
            .Select(static observation => observation.ToDocument())
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
        return new LiveLatestObservationWriteResult(insertedCount, updatedCount, ignoredCount);
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
