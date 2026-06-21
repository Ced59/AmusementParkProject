using AmusementPark.Application.Features.TechnicalPages.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TechnicalPageRepository : ITechnicalPageRepository
{
    private readonly IMongoCollection<TechnicalPageDocument> collection;

    public TechnicalPageRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<TechnicalPageDocument>(settings.TechnicalPagesCollectionName);
    }

    public async Task<IReadOnlyCollection<TechnicalPage>> GetAllAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<TechnicalPageDocument> filter = includeHidden
            ? Builders<TechnicalPageDocument>.Filter.Empty
            : BuildPublicFilter();

        List<TechnicalPageDocument> documents = await this.collection
            .Find(filter)
            .SortBy(document => document.CategoryKey)
            .ThenBy(document => document.SortOrder)
            .ThenBy(document => document.Slug)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<TechnicalPage?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        TechnicalPageDocument? document = await this.collection
            .Find(value => value.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TechnicalPage?> GetBySlugAsync(string slug, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<TechnicalPageDocument> slugFilter = Builders<TechnicalPageDocument>.Filter.Eq(document => document.Slug, slug);
        FilterDefinition<TechnicalPageDocument> filter = includeHidden
            ? slugFilter
            : Builders<TechnicalPageDocument>.Filter.And(slugFilter, BuildPublicFilter());

        TechnicalPageDocument? document = await this.collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<TechnicalPage> CreateAsync(TechnicalPage page, CancellationToken cancellationToken)
    {
        TechnicalPageDocument document = page.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<TechnicalPage?> UpdateAsync(string id, TechnicalPage page, CancellationToken cancellationToken)
    {
        TechnicalPageDocument document = page.ToDocument();
        document.Id = id;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == id,
            document,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 0 ? null : document.ToDomain();
    }

    public async Task<TechnicalPageUpsertOutcome> UpsertBySlugAsync(TechnicalPage page, CancellationToken cancellationToken)
    {
        TechnicalPageDocument document = page.ToDocument();
        TechnicalPageDocument? existing = await this.collection
            .Find(value => value.Slug == document.Slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            document.CreatedAt = DateTime.UtcNow;
            document.UpdatedAt = document.CreatedAt;
            await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
            return new TechnicalPageUpsertOutcome(document.ToDomain(), true);
        }

        document.Id = existing.Id;
        document.CreatedAt = existing.CreatedAt;
        document.UpdatedAt = DateTime.UtcNow;
        await this.collection.ReplaceOneAsync(
            value => value.Id == existing.Id,
            document,
            cancellationToken: cancellationToken);

        return new TechnicalPageUpsertOutcome(document.ToDomain(), false);
    }

    private static FilterDefinition<TechnicalPageDocument> BuildPublicFilter()
    {
        return Builders<TechnicalPageDocument>.Filter.And(
            Builders<TechnicalPageDocument>.Filter.Eq(document => document.IsVisible, true),
            Builders<TechnicalPageDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant));
    }
}
