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
}
