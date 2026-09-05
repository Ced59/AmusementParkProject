using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Version embarquée d'une carte officielle de parc.
/// </summary>
public sealed class ParkOfficialMapDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("format")]
    [BsonRepresentation(BsonType.String)]
    public ParkOfficialMapFormat Format { get; set; }

    [BsonElement("documentUrl")]
    [BsonIgnoreIfNull]
    public string? DocumentUrl { get; set; }

    [BsonElement("storageKey")]
    [BsonIgnoreIfNull]
    public string? StorageKey { get; set; }

    [BsonElement("originalFileName")]
    [BsonIgnoreIfNull]
    public string? OriginalFileName { get; set; }

    [BsonElement("contentType")]
    [BsonIgnoreIfNull]
    public string? ContentType { get; set; }

    [BsonElement("sizeInBytes")]
    [BsonIgnoreIfNull]
    public long? SizeInBytes { get; set; }

    [BsonElement("previewImageUrl")]
    [BsonIgnoreIfNull]
    public string? PreviewImageUrl { get; set; }

    [BsonElement("sourcePageUrl")]
    [BsonIgnoreIfNull]
    public string? SourcePageUrl { get; set; }

    [BsonElement("languageCode")]
    [BsonIgnoreIfNull]
    public string? LanguageCode { get; set; }

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new List<LocalizedTextDocument>();

    [BsonElement("alternativeTexts")]
    public List<LocalizedTextDocument> AlternativeTexts { get; set; } = new List<LocalizedTextDocument>();

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; }

    [BsonElement("lastVerifiedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastVerifiedAtUtc { get; set; }
}
