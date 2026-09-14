using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkFitOperationalStatusRepository : IParkFitOperationalStatusRepository
{
    private readonly IMongoCollection<ParkFitOperationalStatusDocument> collection;

    public ParkFitOperationalStatusRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<ParkFitOperationalStatusDocument>(
            settings.ParkFitOperationalStatusesCollectionName);
    }

    public async Task<ParkFitOperationalStatus?> GetAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        ParkFitOperationalStatusDocument? document = await this.collection
            .Find(ParkFitOperationalStatusMongoDefinitions.BuildParkIdFilter(parkId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyDictionary<string, ParkFitOperationalStatus>> GetByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        if (parkIds.Count == 0)
        {
            return new Dictionary<string, ParkFitOperationalStatus>(StringComparer.Ordinal);
        }

        List<ParkFitOperationalStatusDocument> documents = await this.collection
            .Find(Builders<ParkFitOperationalStatusDocument>.Filter.In(
                static document => document.Id,
                parkIds))
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(
            static document => document.Id,
            static document => document.ToDomain(),
            StringComparer.Ordinal);
    }

    public async Task<ParkFitOperationalStatusWriteOutcome> ReplaceAsync(
        ParkFitOperationalStatus status,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (expectedRevision < 0 || status.Revision != expectedRevision + 1)
        {
            throw new ArgumentException(
                "The status must be exactly one revision ahead of the expected revision.",
                nameof(status));
        }

        try
        {
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                ParkFitOperationalStatusMongoDefinitions.BuildRevisionFilter(
                    status.ParkId,
                    expectedRevision),
                status.ToDocument(),
                new ReplaceOptions { IsUpsert = expectedRevision == 0 },
                cancellationToken);
            return result.MatchedCount == 1 || result.UpsertedId is not null
                ? ParkFitOperationalStatusWriteOutcome.Success
                : ParkFitOperationalStatusWriteOutcome.Conflict;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ParkFitOperationalStatusWriteOutcome.Conflict;
        }
    }
}
