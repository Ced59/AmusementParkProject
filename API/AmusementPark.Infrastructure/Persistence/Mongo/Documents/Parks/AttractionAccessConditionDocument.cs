using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document embarqué d'une contrainte d'accès.
/// </summary>
public sealed class AttractionAccessConditionDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionType Type { get; set; }

    [BsonElement("typeKey")]
    [BsonIgnoreIfNull]
    public string? TypeKey { get; set; }

    [BsonElement("isCustom")]
    [BsonIgnoreIfNull]
    public bool? IsCustom { get; set; }

    [BsonElement("customTypeKey")]
    [BsonIgnoreIfNull]
    public string? CustomTypeKey { get; set; }

    [BsonElement("customTypeLabel")]
    public List<LocalizedTextDocument> CustomTypeLabel { get; set; } = new();

    [BsonElement("value")]
    [BsonIgnoreIfNull]
    public double? Value { get; set; }

    [BsonElement("unit")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionUnit? Unit { get; set; }

    [BsonElement("requiresAccompaniment")]
    [BsonIgnoreIfNull]
    public bool? RequiresAccompaniment { get; set; }

    [BsonElement("minimumCompanionAge")]
    [BsonIgnoreIfNull]
    public int? MinimumCompanionAge { get; set; }

    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();

    [BsonElement("description")]
    public List<LocalizedTextDocument> Description { get; set; } = new();

    [BsonElement("displayOrder")]
    [BsonIgnoreIfNull]
    public int? DisplayOrder { get; set; }

    [BsonElement("provenanceSchemaVersion")]
    public int ProvenanceSchemaVersion { get; set; } = AttractionAccessCondition.CurrentProvenanceSchemaVersion;

    [BsonElement("sourceKind")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionSourceKind SourceKind { get; set; } = AttractionAccessConditionSourceKind.Unknown;

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("sourceReference")]
    [BsonIgnoreIfNull]
    public string? SourceReference { get; set; }

    [BsonElement("collectedAtUtc")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CollectedAtUtc { get; set; }

    [BsonElement("verifiedAtUtc")]
    [BsonIgnoreIfNull]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? VerifiedAtUtc { get; set; }

    [BsonElement("sourceLanguageCode")]
    [BsonIgnoreIfNull]
    public string? SourceLanguageCode { get; set; }

    [BsonElement("sourceSummary")]
    public List<LocalizedTextDocument> SourceSummary { get; set; } = new();

    [BsonElement("sourceConfidence")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionConfidence SourceConfidence { get; set; } = AttractionAccessConditionConfidence.Unknown;

    [BsonElement("scope")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionScope Scope { get; set; } = AttractionAccessConditionScope.Attraction;

    [BsonElement("scopeDetail")]
    [BsonIgnoreIfNull]
    public string? ScopeDetail { get; set; }

    [BsonElement("effectiveFrom")]
    [BsonIgnoreIfNull]
    public string? EffectiveFrom { get; set; }

    [BsonElement("effectiveTo")]
    [BsonIgnoreIfNull]
    public string? EffectiveTo { get; set; }
}
