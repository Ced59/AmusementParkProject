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
