using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class PassportProfileShareSnapshotDocument : MongoDocumentBase
{
    [BsonElement("publicationId")]
    public string PublicationId { get; set; } = string.Empty;

    [BsonElement("publicationVersion")]
    public long PublicationVersion { get; set; }

    [BsonElement("publicationStateVersion")]
    public long PublicationStateVersion { get; set; }

    [BsonElement("sourceVersion")]
    public long SourceVersion { get; set; }

    [BsonElement("policySchemaVersion")]
    public int PolicySchemaVersion { get; set; }

    [BsonElement("datePrecision")]
    [BsonRepresentation(BsonType.String)]
    public ShareDatePrecision DatePrecision { get; set; }

    [BsonElement("includedFields")]
    [BsonRepresentation(BsonType.String)]
    public List<ShareContentField> IncludedFields { get; set; } = new();

    [BsonElement("contentFingerprint")]
    public string ContentFingerprint { get; set; } = string.Empty;

    [BsonElement("selection")]
    public PassportProfileShareSelectionDocument Selection { get; set; } =
        new PassportProfileShareSelectionDocument();

    [BsonElement("content")]
    public PassportProfileShareContentDocument Content { get; set; } =
        new PassportProfileShareContentDocument();
}
