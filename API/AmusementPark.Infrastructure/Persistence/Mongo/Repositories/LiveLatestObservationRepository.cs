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
}
