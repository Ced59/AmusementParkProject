using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.Countries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Base factorisée des repositories Mongo CRUD simples.
/// </summary>
public abstract class MongoCrudRepositoryBase<TDomain, TDocument>
    where TDocument : AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.MongoDocumentBase
{
    private readonly IMongoCollection<TDocument> collection;

    protected MongoCrudRepositoryBase(IMongoCollection<TDocument> collection)
    {
        this.collection = collection;
    }

    protected IMongoCollection<TDocument> Collection => this.collection;

    protected async Task<IReadOnlyCollection<TDomain>> GetAllAsync(Func<TDocument, TDomain> mapper, CancellationToken cancellationToken)
    {
        List<TDocument> documents = await this.collection.Find(Builders<TDocument>.Filter.Empty)
            .Sort(Builders<TDocument>.Sort.Ascending("adminReviewPriority").Ascending("name").Ascending("_id"))
            .ToListAsync(cancellationToken);

        return documents.Select(mapper).ToList();
    }

    protected async Task<TDomain?> GetByIdAsync(string id, Func<TDocument, TDomain> mapper, CancellationToken cancellationToken)
    {
        TDocument? document = await this.collection.Find(document => document.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? default : mapper(document);
    }

    protected async Task<IReadOnlyCollection<TDomain>> GetByIdsAsync(IReadOnlyCollection<string> ids, Func<TDocument, TDomain> mapper, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return Array.Empty<TDomain>();
        }

        List<TDocument> documents = await this.collection
            .Find(Builders<TDocument>.Filter.In(document => document.Id, normalizedIds))
            .ToListAsync(cancellationToken);

        return documents.Select(mapper).ToList();
    }

    protected async Task<TDomain> CreateAsync(TDomain entity, Func<TDomain, TDocument> toDocument, Func<TDocument, TDomain> toDomain, CancellationToken cancellationToken)
    {
        TDocument document = toDocument(entity);
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return toDomain(document);
    }

    protected async Task<TDomain?> UpdateAsync(string id, TDomain entity, Func<TDomain, TDocument> toDocument, Func<TDocument, TDomain> toDomain, CancellationToken cancellationToken)
    {
        TDocument document = toDocument(entity);
        document.Id = id;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == id,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return default;
        }

        return toDomain(document);
    }

    protected async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(
            document => document.Id == id,
            cancellationToken: cancellationToken);

        return result.DeletedCount > 0;
    }

    protected async Task<int> UpdateBulkAdminReviewStatusAsync(IReadOnlyCollection<string> ids, AdminReviewStatus adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return 0;
        }

        AdminReviewStatus normalizedStatus = adminReviewStatus.NormalizeForAdministration();
        UpdateDefinition<TDocument> update = Builders<TDocument>.Update
            .Set("adminReviewStatus", normalizedStatus.ToString())
            .Set("adminReviewPriority", normalizedStatus.ToAdminReviewPriority())
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        UpdateResult result = await this.collection.UpdateManyAsync(
            Builders<TDocument>.Filter.In(document => document.Id, normalizedIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }
}
