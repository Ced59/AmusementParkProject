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
/// Repository Mongo des pays.
/// </summary>
public sealed class CountryReadRepository : ICountryReadRepository
{
    private readonly IMongoCollection<CountryDocument> collection;

    public CountryReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<CountryDocument>(settings.CountriesCollectionName);
    }

    public async Task<IReadOnlyCollection<Country>> GetAllAsync(string? languageCode, CancellationToken cancellationToken)
    {
        List<CountryDocument> documents = await this.collection.Find(Builders<CountryDocument>.Filter.Empty)
            .SortBy(document => document.IsoCode)
            .ToListAsync(cancellationToken);

        List<Country> countries = documents.Select(document => document.ToDomain()).ToList();

        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return countries;
        }

        string normalizedLanguageCode = languageCode.Trim();

        foreach (Country country in countries)
        {
            country.Names = country.Names
                .OrderByDescending(value => string.Equals(value.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase))
                .ThenBy(value => value.LanguageCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return countries;
    }
}

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

/// <summary>
/// Repository Mongo des fondateurs.
/// </summary>
public sealed class ParkFounderRepository : MongoCrudRepositoryBase<ParkFounder, ParkFounderDocument>, IParkFounderRepository
{
    public ParkFounderRepository(IMongoDatabase database, MongoDbSettings settings)
        : base(database.GetCollection<ParkFounderDocument>(settings.ParkFoundersCollectionName))
    {
    }

    public Task<IReadOnlyCollection<ParkFounder>> GetAllAsync(CancellationToken cancellationToken)
    {
        return base.GetAllAsync(document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder> CreateAsync(ParkFounder entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkFounder?> UpdateAsync(string id, ParkFounder entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }
}

/// <summary>
/// Repository Mongo des exploitants.
/// </summary>
public sealed class ParkOperatorRepository : MongoCrudRepositoryBase<ParkOperator, ParkOperatorDocument>, IParkOperatorRepository
{
    public ParkOperatorRepository(IMongoDatabase database, MongoDbSettings settings)
        : base(database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName))
    {
    }

    public Task<IReadOnlyCollection<ParkOperator>> GetAllAsync(CancellationToken cancellationToken)
    {
        return base.GetAllAsync(document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator> CreateAsync(ParkOperator entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<ParkOperator?> UpdateAsync(string id, ParkOperator entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<int> UpdateBulkAdminReviewStatusAsync(IReadOnlyCollection<string> ids, AdminReviewStatus adminReviewStatus, CancellationToken cancellationToken)
    {
        return base.UpdateBulkAdminReviewStatusAsync(ids, adminReviewStatus, cancellationToken);
    }
}

/// <summary>
/// Repository Mongo des constructeurs d'attractions.
/// </summary>
public sealed class AttractionManufacturerRepository : MongoCrudRepositoryBase<AttractionManufacturer, AttractionManufacturerDocument>, IAttractionManufacturerRepository
{
    public AttractionManufacturerRepository(IMongoDatabase database, MongoDbSettings settings)
        : base(database.GetCollection<AttractionManufacturerDocument>(settings.AttractionManufacturersCollectionName))
    {
    }

    public Task<IReadOnlyCollection<AttractionManufacturer>> GetAllAsync(CancellationToken cancellationToken)
    {
        return base.GetAllAsync(document => document.ToDomain(), cancellationToken);
    }

    public async Task<PagedResult<AttractionManufacturer>> GetPageAsync(int page, int pageSize, string? search, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<AttractionManufacturerDocument> filter = BuildManufacturerFilter(search, includeHidden);
        long totalItems = await this.Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<AttractionManufacturerDocument> documents = await this.Collection.Find(filter)
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AttractionManufacturer>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public Task<AttractionManufacturer?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<IReadOnlyCollection<AttractionManufacturer>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
    {
        return base.GetByIdsAsync(ids, document => document.ToDomain(), cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> SearchIdsAsync(string search, bool includeHidden, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(search) || limit <= 0)
        {
            return Array.Empty<string>();
        }

        FilterDefinition<AttractionManufacturerDocument> filter = BuildManufacturerFilter(search, includeHidden);
        List<string> ids = await this.Collection.Find(filter)
            .Project(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public Task<AttractionManufacturer> CreateAsync(AttractionManufacturer entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<AttractionManufacturer?> UpdateAsync(string id, AttractionManufacturer entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return base.DeleteAsync(id, cancellationToken);
    }

    public Task<int> UpdateBulkAdminReviewStatusAsync(IReadOnlyCollection<string> ids, AdminReviewStatus adminReviewStatus, CancellationToken cancellationToken)
    {
        return base.UpdateBulkAdminReviewStatusAsync(ids, adminReviewStatus, cancellationToken);
    }

    private static FilterDefinition<AttractionManufacturerDocument> BuildManufacturerFilter(string? search, bool includeHidden)
    {
        FilterDefinitionBuilder<AttractionManufacturerDocument> filterBuilder = Builders<AttractionManufacturerDocument>.Filter;
        FilterDefinition<AttractionManufacturerDocument> visibilityFilter = includeHidden
            ? Builders<AttractionManufacturerDocument>.Filter.Empty
            : filterBuilder.Or(
                filterBuilder.Eq(document => document.IsVisible, true),
                filterBuilder.Exists("isVisible", false));

        if (string.IsNullOrWhiteSpace(search))
        {
            return visibilityFilter;
        }

        string normalizedSearch = search.Trim();
        BsonRegularExpression regex = new BsonRegularExpression(Regex.Escape(normalizedSearch), "i");
        List<FilterDefinition<AttractionManufacturerDocument>> searchFilters = new List<FilterDefinition<AttractionManufacturerDocument>>
        {
            filterBuilder.Regex(document => document.Name, regex),
            filterBuilder.Regex(document => document.LegalName, regex),
            filterBuilder.Regex("contactDetails.city", regex),
            filterBuilder.Regex("contactDetails.countryCode", regex),
        };

        if (int.TryParse(normalizedSearch, out int year))
        {
            searchFilters.Add(filterBuilder.Eq(document => document.FoundedYear, year));
            searchFilters.Add(filterBuilder.Eq(document => document.ClosedYear, year));
        }

        return visibilityFilter & filterBuilder.Or(searchFilters);
    }
}
