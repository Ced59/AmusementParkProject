using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document Mongo d'un type réutilisable de condition d'accès.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class AttractionAccessConditionTypeDefinitionDocument : MongoDocumentBase
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("legacyType")]
    [BsonRepresentation(BsonType.String)]
    public AttractionAccessConditionType LegacyType { get; set; } = AttractionAccessConditionType.Custom;

    [BsonElement("isSystem")]
    public bool IsSystem { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}
