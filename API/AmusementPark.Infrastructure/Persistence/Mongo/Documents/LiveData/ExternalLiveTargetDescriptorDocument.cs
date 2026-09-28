using AmusementPark.Core.Domain.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class ExternalLiveTargetDescriptorDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public LiveTargetType Type { get; set; }

    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("parentId")]
    [BsonIgnoreIfNull]
    public string? ParentId { get; set; }

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("parentDisplayName")]
    [BsonIgnoreIfNull]
    public string? ParentDisplayName { get; set; }

    [BsonElement("countryCode")]
    public string CountryCode { get; set; } = string.Empty;
}
