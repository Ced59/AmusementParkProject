using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;

[BsonIgnoreExtraElements]
public sealed class ContactGrievanceDocument : MongoDocumentBase
{
    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("languageCode")]
    [BsonIgnoreIfNull]
    public string? LanguageCode { get; set; }

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = "unknown";

    [BsonElement("userAgent")]
    [BsonIgnoreIfNull]
    public string? UserAgent { get; set; }
}
