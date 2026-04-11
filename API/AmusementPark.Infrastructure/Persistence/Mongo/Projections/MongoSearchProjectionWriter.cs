using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
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

    private async Task<SearchItemDocument> BuildParkSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkDocument? source = await this.parksCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park '{resourceId}' not found for search projection.");
        }

        List<string> keywords = this.BuildKeywords(source.Name, source.CountryCode, source.City, source.PostalCode, source.Type?.ToString());

        return new SearchItemDocument
        {
            Id = Guid.NewGuid().ToString(),
            OriginalId = $"park_{source.Id}",
            Category = "park",
            ResourceType = SearchProjectionResourceTypes.Parks,
            Title = source.Name ?? string.Empty,
            Subtitle = BuildParkSubtitle(source),
            Description = this.ResolveLocalizedText(source.Descriptions) ?? this.BuildParkFallbackDescription(source),
            Keywords = keywords,
            CompositeScore = 0.0,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = source.IsVisible,
        };
    }

    private async Task<SearchItemDocument> BuildParkItemSearchItemAsync(string resourceId, CancellationToken cancellationToken)
    {
        ParkItemDocument? source = await this.parkItemsCollection.Find(value => value.Id == resourceId).FirstOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            throw new InvalidOperationException($"Park item '{resourceId}' not found for search projection.");
        }

        ParkDocument? park = await this.parksCollection.Find(value => value.Id == source.ParkId).FirstOrDefaultAsync(cancellationToken);
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
            Description = this.ResolveLocalizedText(source.Descriptions) ?? fallbackDescription,
            Keywords = keywords,
            CompositeScore = 0.0,
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            IsVisible = (park?.IsVisible ?? true) && source.IsVisible,
        };
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
            Description = this.ResolveLocalizedText(source.Biography) ?? source.Name,
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
            Description = this.ResolveLocalizedText(source.Description) ?? source.Name,
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
            Description = this.ResolveLocalizedText(source.Biography) ?? source.Name,
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

    private string? ResolveLocalizedText(IEnumerable<LocalizedTextDocument>? values)
    {
        if (values is null)
        {
            return null;
        }

        List<LocalizedTextDocument> safeValues = values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode))
            .ToList();

        LocalizedTextDocument? english = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, "en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Value));
        if (english is not null)
        {
            return english.Value;
        }

        LocalizedTextDocument? french = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, "fr", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Value));
        if (french is not null)
        {
            return french.Value;
        }

        LocalizedTextDocument? first = safeValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Value));
        return first?.Value;
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
