using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

/// <summary>
/// Projection technique Mongo du moteur de recherche pour les features migrées.
/// </summary>
public sealed class MongoSearchProjectionWriter : ISearchProjectionWriter
{
    private readonly IMongoCollection<SearchItemDocument> searchCollection;
    private readonly IMongoCollection<ParkDocument> parksCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;
    private readonly IMongoCollection<ParkFounderDocument> parkFoundersCollection;
    private readonly IMongoCollection<ParkOperatorDocument> parkOperatorsCollection;
    private readonly IMongoCollection<AttractionManufacturerDocument> attractionManufacturersCollection;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="MongoSearchProjectionWriter"/>.
    /// </summary>
    public MongoSearchProjectionWriter(IMongoDatabase database, MongoDbSettings settings)
    {
        this.searchCollection = database.GetCollection<SearchItemDocument>(settings.SearchItemCollectionName);
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.parkFoundersCollection = database.GetCollection<ParkFounderDocument>(settings.ParkFoundersCollectionName);
        this.parkOperatorsCollection = database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName);
        this.attractionManufacturersCollection = database.GetCollection<AttractionManufacturerDocument>(settings.AttractionManufacturersCollectionName);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("resourceType");
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException("resourceId");
        }

        SearchItemDocument document = resourceType switch
        {
            SearchProjectionResourceTypes.Parks => await this.BuildParkSearchItemAsync(resourceId, cancellationToken),
            SearchProjectionResourceTypes.ParkItems => await this.BuildParkItemSearchItemAsync(resourceId, cancellationToken),
            SearchProjectionResourceTypes.Operators => await this.BuildParkOperatorSearchItemAsync(resourceId, cancellationToken),
            SearchProjectionResourceTypes.Manufacturers => await this.BuildAttractionManufacturerSearchItemAsync(resourceId, cancellationToken),
            SearchProjectionResourceTypes.Founders => await this.BuildParkFounderSearchItemAsync(resourceId, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported search projection resource type '{resourceType}'."),
        };

        FilterDefinition<SearchItemDocument> filter = Builders<SearchItemDocument>.Filter.Eq(value => value.OriginalId, document.OriginalId);
        SearchItemDocument? existing = await this.searchCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            document.Id = existing.Id;
            document.CreatedAt = existing.CreatedAt;
        }

        document.RefreshLocation();
        await this.searchCollection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpsertManyAsync(string resourceType, IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("resourceType");
        }

        ArgumentNullException.ThrowIfNull(resourceIds);

        HashSet<string> distinctIds = resourceIds
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (distinctIds.Count == 0)
        {
            return;
        }

        if (string.Equals(resourceType, SearchProjectionResourceTypes.Parks, StringComparison.Ordinal))
        {
            await this.UpsertManyParksAsync(distinctIds, cancellationToken);
            return;
        }

        if (string.Equals(resourceType, SearchProjectionResourceTypes.ParkItems, StringComparison.Ordinal))
        {
            await this.UpsertManyParkItemsAsync(distinctIds, cancellationToken);
            return;
        }

        foreach (string resourceId in distinctIds)
        {
            await this.UpsertAsync(resourceType, resourceId, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
        {
            return;
        }

        string originalId = resourceType switch
        {
            SearchProjectionResourceTypes.Parks => $"park_{resourceId}",
            SearchProjectionResourceTypes.ParkItems => $"parkItem_{resourceId}",
            SearchProjectionResourceTypes.Operators => $"operator_{resourceId}",
            SearchProjectionResourceTypes.Manufacturers => $"manufacturer_{resourceId}",
            SearchProjectionResourceTypes.Founders => $"founder_{resourceId}",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(originalId))
        {
            return;
        }

        await this.searchCollection.DeleteOneAsync(value => value.OriginalId == originalId, cancellationToken);
    }

    private async Task UpsertManyParksAsync(IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
    {
        List<ParkDocument> parks = await this.parksCollection
            .Find(item => resourceIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (parks.Count == 0)
        {
            return;
        }

        Dictionary<string, int> attractionCountsByParkId = await this.LoadAttractionCountsByParkIdsAsync(parks.Select(item => item.Id).ToList(), cancellationToken);
        List<string> originalIds = parks.Select(item => $"park_{item.Id}").ToList();
        Dictionary<string, SearchItemDocument> existingByOriginalId = await this.LoadExistingSearchItemsByOriginalIdsAsync(originalIds, cancellationToken);
        List<WriteModel<SearchItemDocument>> writes = new List<WriteModel<SearchItemDocument>>(parks.Count);

        foreach (ParkDocument park in parks)
        {
            attractionCountsByParkId.TryGetValue(park.Id, out int attractionCount);
            SearchItemDocument document = this.BuildParkSearchItem(park, attractionCount);
            if (existingByOriginalId.TryGetValue(document.OriginalId, out SearchItemDocument? existing))
            {
                document.Id = existing.Id;
                document.CreatedAt = existing.CreatedAt;
            }

            document.RefreshLocation();
            writes.Add(
                new ReplaceOneModel<SearchItemDocument>(
                    Builders<SearchItemDocument>.Filter.Eq(item => item.OriginalId, document.OriginalId),
                    document)
                {
                    IsUpsert = true,
                });
        }

        await this.searchCollection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    private async Task UpsertManyParkItemsAsync(IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
    {
        List<ParkItemDocument> parkItems = await this.parkItemsCollection
            .Find(item => resourceIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (parkItems.Count == 0)
        {
            return;
        }

        HashSet<string> parkIds = parkItems
            .Where(item => !string.IsNullOrWhiteSpace(item.ParkId))
            .Select(item => item.ParkId)
            .ToHashSet(StringComparer.Ordinal);

        List<ParkDocument> parks = await this.parksCollection
            .Find(item => parkIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        Dictionary<string, ParkDocument> parksById = parks
            .ToDictionary(item => item.Id, item => item, StringComparer.Ordinal);

        List<string> originalIds = parkItems.Select(item => $"parkItem_{item.Id}").ToList();
        Dictionary<string, SearchItemDocument> existingByOriginalId = await this.LoadExistingSearchItemsByOriginalIdsAsync(originalIds, cancellationToken);
        List<WriteModel<SearchItemDocument>> writes = new List<WriteModel<SearchItemDocument>>(parkItems.Count);

        foreach (ParkItemDocument parkItem in parkItems)
        {
            parksById.TryGetValue(parkItem.ParkId, out ParkDocument? park);
            SearchItemDocument document = this.BuildParkItemSearchItem(parkItem, park);
            if (existingByOriginalId.TryGetValue(document.OriginalId, out SearchItemDocument? existing))
            {
                document.Id = existing.Id;
                document.CreatedAt = existing.CreatedAt;
            }

            document.RefreshLocation();
            writes.Add(
                new ReplaceOneModel<SearchItemDocument>(
                    Builders<SearchItemDocument>.Filter.Eq(item => item.OriginalId, document.OriginalId),
                    document)
                {
                    IsUpsert = true,
                });
        }

        await this.searchCollection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    private async Task<Dictionary<string, int>> LoadAttractionCountsByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        if (parkIds.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(item => item.ParkId, parkIds)
            & Builders<ParkItemDocument>.Filter.Eq(item => item.Category, AmusementPark.Core.Domain.Parks.ParkItemCategory.Attraction)
            & Builders<ParkItemDocument>.Filter.Eq(item => item.IsVisible, true);

        List<ParkItemDocument> attractions = await this.parkItemsCollection
            .Find(filter)
            .Project(item => new ParkItemDocument { Id = item.Id, ParkId = item.ParkId })
            .ToListAsync(cancellationToken);

        return attractions
            .GroupBy(item => item.ParkId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, SearchItemDocument>> LoadExistingSearchItemsByOriginalIdsAsync(
        IReadOnlyCollection<string> originalIds,
        CancellationToken cancellationToken)
    {
        List<SearchItemDocument> existingItems = await this.searchCollection
            .Find(item => originalIds.Contains(item.OriginalId))
            .ToListAsync(cancellationToken);

        return existingItems.ToDictionary(item => item.OriginalId, item => item, StringComparer.Ordinal);
    }

    private SearchItemDocument BuildParkSearchItem(ParkDocument source, int attractionCount)
    {
        List<string> keywords = this.BuildKeywords(source.Name, source.CountryCode, source.City, source.PostalCode, source.Type?.ToString());

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"park_{source.Id}",
            Category = "park",
            ResourceType = SearchProjectionResourceTypes.Parks,
            Title = source.Name ?? string.Empty,
            Subtitle = BuildParkSubtitle(source),
            Description = SearchLocalizedTextResolver.Resolve(source.Descriptions, "en") ?? this.BuildParkFallbackDescription(source),
            LocalizedDescriptions = SearchLocalizedTextResolver.Normalize(source.Descriptions),
            City = source.City,
            CountryCode = source.CountryCode,
            LogoImageId = source.CurrentLogoImageId,
            AttractionCount = attractionCount,
            Keywords = keywords,
            CompositeScore = 0.0,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = source.IsVisible,
        };
    }

    private SearchItemDocument BuildParkItemSearchItem(ParkItemDocument source, ParkDocument? park)
    {
        string parkName = park?.Name ?? string.Empty;
        string category = ToSearchCategory(source.Category.ToString());
        string typeTag = HumanizeValue(source.Type.ToString());

        List<string> keywords = this.BuildKeywords(source.Name, source.Subtype, source.Type.ToString(), typeTag, source.Category.ToString(), category, parkName, source.AttractionDetails?.Model);
        string fallbackDescription = BuildParkItemFallbackDescription(parkName, category, typeTag, source.Subtype);

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"parkItem_{source.Id}",
            Category = category,
            ResourceType = SearchProjectionResourceTypes.ParkItems,
            Title = source.Name,
            Subtitle = string.IsNullOrWhiteSpace(parkName) ? HumanizeValue(source.Category.ToString()) : parkName,
            Description = SearchLocalizedTextResolver.Resolve(source.Descriptions, "en") ?? fallbackDescription,
            LocalizedDescriptions = SearchLocalizedTextResolver.Normalize(source.Descriptions),
            City = park?.City,
            CountryCode = park?.CountryCode,
            LogoImageId = park?.CurrentLogoImageId,
            ParentParkId = park?.Id,
            ParentParkName = parkName,
            Keywords = keywords,
            CompositeScore = 0.0,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = (park?.IsVisible ?? true) && source.IsVisible,
        };
    }

    private async Task<SearchItemDocument> BuildParkSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkDocument? source = await this.parksCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park '{resourceId}' not found for search projection.");
        }

        Dictionary<string, int> attractionCountsByParkId = await this.LoadAttractionCountsByParkIdsAsync(new[] { source.Id }, cancellationToken);
        attractionCountsByParkId.TryGetValue(source.Id, out int attractionCount);
        return this.BuildParkSearchItem(source, attractionCount);
    }

    private async Task<SearchItemDocument> BuildParkItemSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkItemDocument? source = await this.parkItemsCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park item '{resourceId}' not found for search projection.");
        }

        ParkDocument? park = await this.parksCollection.Find(value => value.Id == source.ParkId).FirstOrDefaultAsync(cancellationToken);
        return this.BuildParkItemSearchItem(source, park);
    }

    private async Task<SearchItemDocument> BuildParkFounderSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkFounderDocument? source = await this.parkFoundersCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park founder '{resourceId}' not found for search projection.");
        }

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"founder_{source.Id}",
            Category = "founders",
            ResourceType = SearchProjectionResourceTypes.Founders,
            Title = source.Name,
            Description = SearchLocalizedTextResolver.Resolve(source.Biography, "en") ?? source.Name,
            LocalizedDescriptions = SearchLocalizedTextResolver.Normalize(source.Biography),
            Keywords = this.BuildKeywords(source.Name, "founder", "fondateur"),
            CompositeScore = 0.0,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = true,
        };
    }

    private async Task<SearchItemDocument> BuildParkOperatorSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkOperatorDocument? source = await this.parkOperatorsCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park operator '{resourceId}' not found for search projection.");
        }

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"operator_{source.Id}",
            Category = "operators",
            ResourceType = SearchProjectionResourceTypes.Operators,
            Title = source.Name,
            Description = SearchLocalizedTextResolver.Resolve(source.Description, "en") ?? source.Name,
            LocalizedDescriptions = SearchLocalizedTextResolver.Normalize(source.Description),
            Keywords = this.BuildKeywords(source.Name, "operator", "exploitant"),
            CompositeScore = 0.0,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = true,
        };
    }

    private async Task<SearchItemDocument> BuildAttractionManufacturerSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        AttractionManufacturerDocument? source = await this.attractionManufacturersCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Attraction manufacturer '{resourceId}' not found for search projection.");
        }

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"manufacturer_{source.Id}",
            Category = "manufacturers",
            ResourceType = SearchProjectionResourceTypes.Manufacturers,
            Title = source.Name,
            Description = SearchLocalizedTextResolver.Resolve(source.Biography, "en") ?? source.Name,
            LocalizedDescriptions = SearchLocalizedTextResolver.Normalize(source.Biography),
            Keywords = this.BuildKeywords(source.Name, "manufacturer", "constructor", "fabricant"),
            CompositeScore = 0.0,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = true,
        };
    }

    private List<string> BuildKeywords(params string?[] values)
    {
        List<string> keywords = new List<string>();

        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (string keyword in ExpandKeywordVariants(value))
            {
                if (!keywords.Contains(keyword, StringComparer.Ordinal))
                {
                    keywords.Add(keyword);
                }
            }
        }

        return keywords;
    }

    private static IEnumerable<string> ExpandKeywordVariants(string value)
    {
        string raw = value.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            yield break;
        }

        string normalized = raw.ToLowerInvariant();
        yield return normalized;

        string humanized = HumanizeValue(raw);
        if (!string.Equals(humanized, normalized, StringComparison.Ordinal))
        {
            yield return humanized;
        }

        string compact = humanized.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!string.Equals(compact, normalized, StringComparison.Ordinal) && !string.Equals(compact, humanized, StringComparison.Ordinal))
        {
            yield return compact;
        }
    }

    private string BuildParkFallbackDescription(ParkDocument park)
    {
        List<string> parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(park.City))
        {
            parts.Add(park.City.Trim());
        }

        if (!string.IsNullOrWhiteSpace(park.CountryCode))
        {
            parts.Add(park.CountryCode.Trim().ToUpperInvariant());
        }

        return parts.Count > 0 ? string.Join(" • ", parts) : (park.Name ?? string.Empty);
    }

    private static string? BuildParkSubtitle(ParkDocument source)
    {
        List<string> parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(source.City))
        {
            parts.Add(source.City.Trim());
        }

        if (!string.IsNullOrWhiteSpace(source.CountryCode))
        {
            parts.Add(source.CountryCode.Trim().ToUpperInvariant());
        }

        if (parts.Count == 0)
        {
            return null;
        }

        return string.Join(" • ", parts);
    }

    private static string BuildParkItemFallbackDescription(string parkName, string category, string typeTag, string? subtype)
    {
        List<string> parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(parkName))
        {
            parts.Add(parkName.Trim());
        }

        parts.Add(typeTag);

        if (!string.IsNullOrWhiteSpace(subtype))
        {
            parts.Add(subtype.Trim());
        }
        else if (!string.Equals(category, typeTag, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(category);
        }

        return string.Join(" • ", parts);
    }

    private static string ToSearchCategory(string value)
    {
        return HumanizeValue(value);
    }

    private static string HumanizeValue(string value)
    {
        string normalized = Regex.Replace(value.Trim(), "([a-z0-9])([A-Z])", "$1 $2");
        normalized = normalized.Replace("_", " ", StringComparison.Ordinal);
        normalized = normalized.Replace("-", " ", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, "\\s+", " ").Trim().ToLowerInvariant();
        return normalized;
    }
}
