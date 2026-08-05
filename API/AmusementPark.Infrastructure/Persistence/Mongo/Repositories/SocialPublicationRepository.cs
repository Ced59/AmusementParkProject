using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class SocialPublicationRepository : ISocialPublicationRepository
{
    private readonly IMongoCollection<SocialPublicationDocument> collection;

    public SocialPublicationRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<SocialPublicationDocument>(settings.SocialPublicationsCollectionName);
    }

    public async Task<SocialPublication> CreateAsync(SocialPublication publication, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        SocialPublicationDocument document = publication.ToDocument();
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<SocialPublication> UpdateAsync(SocialPublication publication, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        SocialPublicationDocument document = publication.ToDocument();
        await this.collection.ReplaceOneAsync(
            current => current.Id == document.Id,
            document,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);
        return document.ToDomain();
    }

    public async Task<SocialPublication?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        SocialPublicationDocument? document = await this.collection
            .Find(current => current.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<SocialPublication?> GetByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken)
    {
        SocialPublicationDocument? document = await this.collection
            .Find(current => current.DeduplicationKey == deduplicationKey)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<SocialPublication?> GetByExternalPostIdAsync(string externalPostId, CancellationToken cancellationToken)
    {
        SocialPublicationDocument? document = await this.collection
            .Find(current => current.ExternalPostId == externalPostId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<SocialPublication>> ListRecentAsync(int limit, CancellationToken cancellationToken)
    {
        List<SocialPublicationDocument> documents = await this.collection
            .Find(Builders<SocialPublicationDocument>.Filter.Empty)
            .SortByDescending(static document => document.RequestedAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList();
    }
}
