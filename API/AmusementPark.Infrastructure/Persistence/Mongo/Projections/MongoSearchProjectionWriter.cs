using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

/// <summary>
/// Projection technique Mongo du moteur de recherche pour les features simples migrées.
/// </summary>
public sealed class MongoSearchProjectionWriter : ISearchProjectionWriter
{
    private readonly IMongoCollection<SearchItemDocument> searchCollection;
    private readonly IMongoCollection<ParkOperatorDocument> parkOperatorsCollection;
    private readonly IMongoCollection<AttractionManufacturerDocument> attractionManufacturersCollection;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="MongoSearchProjectionWriter"/>.
    /// </summary>
    public MongoSearchProjectionWriter(IMongoDatabase database, MongoDbSettings settings)
    {
        this.searchCollection = database.GetCollection<SearchItemDocument>(settings.SearchItemCollectionName);
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
            "operators" => await this.BuildParkOperatorSearchItemAsync(resourceId, cancellationToken),
            "manufacturers" => await this.BuildAttractionManufacturerSearchItemAsync(resourceId, cancellationToken),
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
    public async Task DeleteAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
        {
            return;
        }

        string originalId = resourceType switch
        {
            "operators" => $"operator_{resourceId}",
            "manufacturers" => $"manufacturer_{resourceId}",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(originalId))
        {
            return;
        }

        await this.searchCollection.DeleteOneAsync(value => value.OriginalId == originalId, cancellationToken);
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
            ResourceType = "operators",
            Title = source.Name,
            Description = this.ResolveLocalizedText(source.Description) ?? source.Name,
            Keywords = this.BuildKeywords(source.Name, "operator"),
            CompositeScore = 0.0,
            Latitude = 0.0,
            Longitude = 0.0,
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(0.0, 0.0)),
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
            ResourceType = "manufacturers",
            Title = source.Name,
            Description = this.ResolveLocalizedText(source.Biography) ?? source.Name,
            Keywords = this.BuildKeywords(source.Name, "manufacturer", "constructor"),
            CompositeScore = 0.0,
            Latitude = 0.0,
            Longitude = 0.0,
            Location = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(0.0, 0.0)),
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

            string normalized = value.Trim().ToLowerInvariant();
            if (!keywords.Contains(normalized, StringComparer.Ordinal))
            {
                keywords.Add(normalized);
            }
        }

        return keywords;
    }

    private string? ResolveLocalizedText(IEnumerable<AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.LocalizedTextDocument>? values)
    {
        if (values is null)
        {
            return null;
        }

        List<AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.LocalizedTextDocument> safeValues = values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode))
            .ToList();

        AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.LocalizedTextDocument? english = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, "en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Value));
        if (english is not null)
        {
            return english.Value;
        }

        AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.LocalizedTextDocument? french = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, "fr", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Value));
        if (french is not null)
        {
            return french.Value;
        }

        AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common.LocalizedTextDocument? first = safeValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Value));
        return first?.Value;
    }
}
