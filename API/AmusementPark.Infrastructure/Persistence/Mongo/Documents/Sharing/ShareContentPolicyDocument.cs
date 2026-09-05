using AmusementPark.Core.Domain.Sharing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

public sealed class ShareContentPolicyDocument
{
    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; }

    [BsonElement("datePrecision")]
    [BsonRepresentation(BsonType.String)]
    public ShareDatePrecision DatePrecision { get; set; }

    [BsonElement("includedFields")]
    public List<ShareContentField> IncludedFields { get; set; } = new List<ShareContentField>();
}
