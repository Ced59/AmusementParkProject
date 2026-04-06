using AmusementPark.Application.Features.CaptainCoaster.Ports;
using AmusementPark.Application.Features.CaptainCoaster.Results;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des paramètres Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSettingsRepository : ICaptainCoasterSettingsRepository
{
    private readonly IMongoCollection<CaptainCoasterSettingsDocument> collection;

    public CaptainCoasterSettingsRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<CaptainCoasterSettingsDocument>(settings.CaptainCoasterSettingsCollectionName);
    }

    public async Task<CaptainCoasterSettingsResult> GetAsync(CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument? document = await this.collection.Find(Builders<CaptainCoasterSettingsDocument>.Filter.Empty)
            .SortBy(document => document.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return new CaptainCoasterSettingsResult();
        }

        return document.ToResult();
    }

    public async Task<CaptainCoasterSettingsResult> UpdateAsync(CaptainCoasterSettingsResult settings, CancellationToken cancellationToken)
    {
        CaptainCoasterSettingsDocument existing = await this.collection.Find(Builders<CaptainCoasterSettingsDocument>.Filter.Empty)
            .SortBy(document => document.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? new CaptainCoasterSettingsDocument();

        CaptainCoasterSettingsDocument document = settings.ToDocument();
        document.Id = existing.Id;
        document.CreatedAt = existing.CreatedAt;
        document.UpdatedAt = DateTime.UtcNow;

        FilterDefinition<CaptainCoasterSettingsDocument> filter = Builders<CaptainCoasterSettingsDocument>.Filter.Eq(value => value.Id, document.Id);
        ReplaceOptions options = new ReplaceOptions
        {
            IsUpsert = true,
        };

        await this.collection.ReplaceOneAsync(filter, document, options, cancellationToken);
        return document.ToResult();
    }
}

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
