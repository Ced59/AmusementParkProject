using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des parcs.
/// </summary>
public sealed class ParkRepository : IParkRepository
{
    private readonly IMongoCollection<ParkDocument> collection;

    public ParkRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
    }

    public async Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId);

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        ParkDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<Park>();
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(document => document.Id, normalizedParkIds);
        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = includeHidden
            ? Builders<ParkDocument>.Filter.Empty
            : Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<long> CountAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden);
        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }


    public async Task<IReadOnlyCollection<string>> GetVisibleParkIdsAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);

        List<string> parkIds = await this.collection.Find(filter)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<int> CountDistinctCountryCodesAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, string.Empty);

        IAsyncCursor<string?> cursor = await this.collection.DistinctAsync(
            document => document.CountryCode,
            filter,
            cancellationToken: cancellationToken);

        List<string?> countryCodes = await cursor.ToListAsync(cancellationToken);
        return countryCodes
            .Where(static countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(static countryCode => countryCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    public async Task<int> CountDistinctCountryCodesForParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(document => document.Id, normalizedParkIds)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, string.Empty);

        IAsyncCursor<string?> cursor = await this.collection.DistinctAsync(
            document => document.CountryCode,
            filter,
            cancellationToken: cancellationToken);

        List<string?> countryCodes = await cursor.ToListAsync(cancellationToken);
        return countryCodes
            .Where(static countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(static countryCode => countryCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    public async Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
    {
        string escapedName = Regex.Escape(name.Trim());
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Regex(
            document => document.Name,
            new BsonRegularExpression(escapedName, "i"));

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, CancellationToken cancellationToken)
    {
        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
            new GeoJson2DGeographicCoordinates(longitude, latitude));

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.NearSphere(
            document => document.Location,
            center,
            maxDistance: radiusInKilometers * 1000d);

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<Park> CreateAsync(Park park, CancellationToken cancellationToken)
    {
        ParkDocument document = park.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken)
    {
        ParkDocument document = park.ToDocument();
        document.Id = parkId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == parkId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId);
        UpdateDefinition<ParkDocument> update = Builders<ParkDocument>.Update
            .Set(document => document.IsVisible, isVisible)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ParkDocument> options = new FindOneAndUpdateOptions<ParkDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ParkDocument? updated = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return updated?.ToDomain();
    }

    private FilterDefinition<ParkDocument> BuildVisibilityFilter(bool includeHidden)
    {
        return includeHidden
            ? Builders<ParkDocument>.Filter.Empty
            : Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
    }
}
