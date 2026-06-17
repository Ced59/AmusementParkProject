using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using MongoDB.Bson;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static Video ToDomain(this VideoDocument document)
    {
        Video entity = new Video
        {
            Id = document.Id,
            HostingProvider = document.HostingProvider,
            OwnerType = document.OwnerType,
            OwnerId = document.OwnerId,
            Type = document.Type,
            OriginalUrl = document.OriginalUrl,
            CanonicalUrl = document.CanonicalUrl,
            EmbedUrl = document.EmbedUrl,
            ExternalId = document.ExternalId,
            Title = document.Title,
            Description = document.Description,
            CreatorName = document.CreatorName,
            CreatorUrl = document.CreatorUrl,
            ThumbnailUrl = document.ThumbnailUrl,
            ThumbnailImageId = document.ThumbnailImageId,
            Duration = document.DurationSeconds.HasValue ? TimeSpan.FromSeconds(document.DurationSeconds.Value) : null,
            PublishedAtUtc = document.PublishedAtUtc,
            LanguageCodes = document.LanguageCodes,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            TagIds = document.TagIds,
            ExternalMetadata = document.ExternalMetadata.ToDomain(),
            IsPublished = document.IsPublished,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static Video ToVideoDomain(this BsonDocument document)
    {
        Video entity = new Video
        {
            Id = ReadDocumentId(document),
            HostingProvider = ReadEnum(document, "hostingProvider", VideoHostingProvider.Other),
            OwnerType = ReadEnum(document, "ownerType", VideoOwnerType.None),
            OwnerId = ReadOptionalString(document, "ownerId"),
            Type = ReadEnum(document, "type", VideoType.Other),
            OriginalUrl = ReadString(document, "originalUrl"),
            CanonicalUrl = ReadString(document, "canonicalUrl"),
            EmbedUrl = ReadOptionalString(document, "embedUrl"),
            ExternalId = ReadOptionalString(document, "externalId"),
            Title = ReadString(document, "title"),
            Description = ReadOptionalString(document, "description"),
            CreatorName = ReadOptionalString(document, "creatorName"),
            CreatorUrl = ReadOptionalString(document, "creatorUrl"),
            ThumbnailUrl = ReadOptionalString(document, "thumbnailUrl"),
            ThumbnailImageId = ReadOptionalString(document, "thumbnailImageId"),
            Duration = ReadLong(document, "durationSeconds") is long durationSeconds ? TimeSpan.FromSeconds(durationSeconds) : null,
            PublishedAtUtc = ReadDateTime(document, "publishedAtUtc"),
            LanguageCodes = ReadStringList(document, "languageCodes"),
            Titles = ReadLocalizedTexts(document, "titles"),
            Descriptions = ReadLocalizedTexts(document, "descriptions"),
            TagIds = ReadStringList(document, "tagIds"),
            ExternalMetadata = ReadVideoExternalMetadata(document),
            IsPublished = ReadBoolean(document, "isPublished", true),
        };

        entity.CreatedAtUtc = ReadDateTime(document, "createdAt") ?? DateTime.UtcNow;
        entity.UpdatedAtUtc = ReadDateTime(document, "updatedAt") ?? entity.CreatedAtUtc;
        return entity;
    }

    public static VideoDocument ToDocument(this Video entity)
    {
        return new VideoDocument
        {
            Id = entity.Id,
            HostingProvider = entity.HostingProvider,
            OwnerType = entity.OwnerType,
            OwnerId = entity.OwnerId,
            Type = entity.Type,
            OriginalUrl = entity.OriginalUrl,
            CanonicalUrl = entity.CanonicalUrl,
            EmbedUrl = entity.EmbedUrl,
            ExternalId = entity.ExternalId,
            Title = entity.Title,
            Description = entity.Description,
            CreatorName = entity.CreatorName,
            CreatorUrl = entity.CreatorUrl,
            ThumbnailUrl = entity.ThumbnailUrl,
            ThumbnailImageId = entity.ThumbnailImageId,
            DurationSeconds = entity.Duration.HasValue ? checked((long)entity.Duration.Value.TotalSeconds) : null,
            PublishedAtUtc = entity.PublishedAtUtc,
            LanguageCodes = entity.LanguageCodes,
            Titles = CommonMongoMappers.ToDocuments(entity.Titles),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            TagIds = entity.TagIds,
            ExternalMetadata = entity.ExternalMetadata.ToDocument(),
            IsPublished = entity.IsPublished,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    public static VideoExternalMetadata ToDomain(this VideoExternalMetadataDocument document)
    {
        return new VideoExternalMetadata
        {
            Source = document.Source,
            FetchedAtUtc = document.FetchedAtUtc,
            ProviderTitle = document.ProviderTitle,
            ProviderDescription = document.ProviderDescription,
            ProviderChannelId = document.ProviderChannelId,
            ProviderChannelUrl = document.ProviderChannelUrl,
            ProviderViewCount = document.ProviderViewCount,
        };
    }

    public static VideoExternalMetadataDocument ToDocument(this VideoExternalMetadata entity)
    {
        return new VideoExternalMetadataDocument
        {
            Source = entity.Source,
            FetchedAtUtc = entity.FetchedAtUtc,
            ProviderTitle = entity.ProviderTitle,
            ProviderDescription = entity.ProviderDescription,
            ProviderChannelId = entity.ProviderChannelId,
            ProviderChannelUrl = entity.ProviderChannelUrl,
            ProviderViewCount = entity.ProviderViewCount,
        };
    }

    public static VideoTag ToDomain(this VideoTagDocument document)
    {
        VideoTag entity = new VideoTag
        {
            Id = document.Id,
            Slug = document.Slug,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
            Descriptions = CommonMongoMappers.ToDomain(document.Descriptions),
            IsActive = document.IsActive,
        };

        entity.CreatedAtUtc = document.CreatedAt;
        entity.UpdatedAtUtc = document.UpdatedAt;
        return entity;
    }

    public static VideoTag ToVideoTagDomain(this BsonDocument document)
    {
        VideoTag entity = new VideoTag
        {
            Id = ReadDocumentId(document),
            Slug = ReadString(document, "slug"),
            Labels = ReadLocalizedTexts(document, "labels"),
            Descriptions = ReadLocalizedTexts(document, "descriptions"),
            IsActive = ReadBoolean(document, "isActive", true),
        };

        entity.CreatedAtUtc = ReadDateTime(document, "createdAt") ?? DateTime.UtcNow;
        entity.UpdatedAtUtc = ReadDateTime(document, "updatedAt") ?? entity.CreatedAtUtc;
        return entity;
    }

    public static VideoTagDocument ToDocument(this VideoTag entity)
    {
        return new VideoTagDocument
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Labels = CommonMongoMappers.ToDocuments(entity.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(entity.Descriptions),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
        };
    }

    private static VideoExternalMetadata ReadVideoExternalMetadata(BsonDocument document)
    {
        BsonDocument metadataDocument = ReadDocument(document, "externalMetadata");
        return new VideoExternalMetadata
        {
            Source = ReadOptionalString(metadataDocument, "source"),
            FetchedAtUtc = ReadDateTime(metadataDocument, "fetchedAtUtc"),
            ProviderTitle = ReadOptionalString(metadataDocument, "providerTitle"),
            ProviderDescription = ReadOptionalString(metadataDocument, "providerDescription"),
            ProviderChannelId = ReadOptionalString(metadataDocument, "providerChannelId"),
            ProviderChannelUrl = ReadOptionalString(metadataDocument, "providerChannelUrl"),
            ProviderViewCount = ReadLong(metadataDocument, "providerViewCount"),
        };
    }

    private static string ReadDocumentId(BsonDocument document)
    {
        if (!document.TryGetValue("_id", out BsonValue? idValue) || IsNullLike(idValue))
        {
            return string.Empty;
        }

        return idValue switch
        {
            { IsString: true } => idValue.AsString,
            { IsObjectId: true } => idValue.AsObjectId.ToString(),
            _ => idValue.ToString() ?? string.Empty,
        };
    }

    private static string ReadString(BsonDocument document, string fieldName)
    {
        return ReadOptionalString(document, fieldName) ?? string.Empty;
    }

    private static string? ReadOptionalString(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsString)
        {
            string normalizedValue = value.AsString.Trim();
            return normalizedValue.Length > 0 ? normalizedValue : null;
        }

        return value.ToString();
    }

    private static bool ReadBoolean(BsonDocument document, string fieldName, bool defaultValue)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return defaultValue;
        }

        if (value.IsBoolean)
        {
            return value.AsBoolean;
        }

        if (value.IsString && bool.TryParse(value.AsString, out bool parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static long? ReadLong(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsInt64)
        {
            return value.AsInt64;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsDouble)
        {
            return checked((long)value.AsDouble);
        }

        if (value.IsString && long.TryParse(value.AsString, out long parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTime? ReadDateTime(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || IsNullLike(value))
        {
            return null;
        }

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        if (value.IsString && DateTime.TryParse(value.AsString, out DateTime parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc) : parsed.ToUniversalTime();
        }

        return null;
    }

    private static TEnum ReadEnum<TEnum>(BsonDocument document, string fieldName, TEnum defaultValue)
        where TEnum : struct, Enum
    {
        string? rawValue = ReadOptionalString(document, fieldName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (Enum.TryParse(rawValue, true, out TEnum parsed))
        {
            return parsed;
        }

        string normalizedValue = NormalizeEnumToken(rawValue);
        foreach (string enumName in Enum.GetNames<TEnum>())
        {
            if (string.Equals(NormalizeEnumToken(enumName), normalizedValue, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<TEnum>(enumName);
            }
        }

        return defaultValue;
    }

    private static string NormalizeEnumToken(string value)
    {
        return value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static List<string> ReadStringList(BsonDocument document, string fieldName)
    {
        List<string> values = new List<string>();
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonArray)
        {
            return values;
        }

        foreach (BsonValue item in value.AsBsonArray)
        {
            if (item.IsString && !string.IsNullOrWhiteSpace(item.AsString))
            {
                values.Add(item.AsString.Trim());
            }
        }

        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<LocalizedText> ReadLocalizedTexts(BsonDocument document, string fieldName)
    {
        List<LocalizedText> values = new List<LocalizedText>();
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonArray)
        {
            return values;
        }

        foreach (BsonValue item in value.AsBsonArray)
        {
            if (!item.IsBsonDocument)
            {
                continue;
            }

            BsonDocument localizedDocument = item.AsBsonDocument;
            string languageCode = ReadString(localizedDocument, "languageCode");
            string? localizedValue = ReadOptionalString(localizedDocument, "value");
            if (!string.IsNullOrWhiteSpace(languageCode) && !string.IsNullOrWhiteSpace(localizedValue))
            {
                values.Add(new LocalizedText(languageCode.Trim().ToLowerInvariant(), localizedValue.Trim()));
            }
        }

        return values;
    }

    private static BsonDocument ReadDocument(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out BsonValue? value) || !value.IsBsonDocument)
        {
            return new BsonDocument();
        }

        return value.AsBsonDocument;
    }

    private static bool IsNullLike(BsonValue value)
    {
        return value.IsBsonNull || value.BsonType == BsonType.Undefined;
    }
}
