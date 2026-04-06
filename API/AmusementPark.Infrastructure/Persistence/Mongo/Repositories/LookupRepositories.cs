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
            .Sort(Builders<TDocument>.Sort.Ascending("name").Ascending("_id"))
            .ToListAsync(cancellationToken);

        return documents.Select(mapper).ToList();
    }

    protected async Task<TDomain?> GetByIdAsync(string id, Func<TDocument, TDomain> mapper, CancellationToken cancellationToken)
    {
        TDocument? document = await this.collection.Find(document => document.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? default : mapper(document);
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

    public Task<AttractionManufacturer?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return base.GetByIdAsync(id, document => document.ToDomain(), cancellationToken);
    }

    public Task<AttractionManufacturer> CreateAsync(AttractionManufacturer entity, CancellationToken cancellationToken)
    {
        return base.CreateAsync(entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }

    public Task<AttractionManufacturer?> UpdateAsync(string id, AttractionManufacturer entity, CancellationToken cancellationToken)
    {
        return base.UpdateAsync(id, entity, value => value.ToDocument(), document => document.ToDomain(), cancellationToken);
    }
}
