using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveHistoryRepository : ILiveHistoryRepository
{
    private readonly IMongoCollection<LiveLatestObservationDocument> rawCollection;
    private readonly IMongoCollection<LiveHistoryBucketDocument> bucketCollection;

    public LiveHistoryRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.rawCollection = database.GetCollection<LiveLatestObservationDocument>(
            settings.LiveRawHistoryCollectionName);
        this.bucketCollection = database.GetCollection<LiveHistoryBucketDocument>(
            settings.LiveHistoryBucketsCollectionName);
    }

    public async Task StoreAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        LiveHistoryRetentionPolicy retentionPolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(retentionPolicy);
        if (observations.Count == 0)
        {
            return;
        }

        LiveLatestObservation[] uniqueObservations = observations
            .GroupBy(static observation => (
                observation.Provenance.SourceId,
                observation.Target.Type,
                observation.Target.Id,
                observation.Provenance.ObservedAtUtc.Ticks))
            .Select(static group => group
                .OrderBy(static observation => observation.Provenance.ReceivedAtUtc)
                .Last())
            .ToArray();
        List<UpdateOneModel<LiveLatestObservationDocument>> rawWrites = uniqueObservations
            .Select(observation => observation.ToRawHistoryDocument(retentionPolicy))
            .Select(document => new UpdateOneModel<LiveLatestObservationDocument>(
                LiveLatestObservationMongoDefinitions.BuildNaturalKeyFilter(document),
                LiveLatestObservationMongoDefinitions.BuildMonotonicUpdate(document))
            {
                IsUpsert = true,
            })
            .ToList();
        await this.rawCollection.BulkWriteAsync(
            rawWrites,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);

        List<UpdateOneModel<LiveHistoryBucketDocument>> bucketWrites = uniqueObservations
            .Select(observation => observation.ToHistoryBucketDocument(retentionPolicy))
            .Select(document => new UpdateOneModel<LiveHistoryBucketDocument>(
                Builders<LiveHistoryBucketDocument>.Filter.Eq(
                    static candidate => candidate.Id,
                    document.Id),
                LiveHistoryMongoDefinitions.BuildBucketUpdate(document))
            {
                IsUpsert = true,
            })
            .ToList();
        await this.bucketCollection.BulkWriteAsync(
            bucketWrites,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
    }
}
