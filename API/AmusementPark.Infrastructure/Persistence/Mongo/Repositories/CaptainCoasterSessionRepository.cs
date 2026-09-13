using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des sessions Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSessionRepository : ICaptainCoasterSessionRepository
{
    private readonly IMongoCollection<CaptainCoasterSyncSessionDocument> collection;

    public CaptainCoasterSessionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<CaptainCoasterSyncSessionDocument>(settings.CaptainCoasterSyncSessionsCollectionName);
    }

    public async Task<CaptainCoasterSessionResult?> GetByIdAsync(string sessionId, CancellationToken cancellationToken)
    {
        CaptainCoasterSyncSessionDocument? document = await this.collection.Find(document => document.Id == sessionId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToResult();
    }
}
