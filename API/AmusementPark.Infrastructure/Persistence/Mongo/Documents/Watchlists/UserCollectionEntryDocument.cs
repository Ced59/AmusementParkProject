using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class UserCollectionEntryDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("targetType")]
    public CollectionTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("kind")]
    public UserCollectionKind Kind { get; set; }

    [BsonElement("targetStatus")]
    public CollectionTargetStatus TargetStatus { get; set; }

    [BsonElement("privateNote")]
    [BsonIgnoreIfNull]
    public string? PrivateNote { get; set; }

    [BsonElement("priority")]
    [BsonIgnoreIfNull]
    public int? Priority { get; set; }

    [BsonElement("preferredStartsOn")]
    [BsonIgnoreIfNull]
    public string? PreferredStartsOn { get; set; }

    [BsonElement("preferredEndsOn")]
    [BsonIgnoreIfNull]
    public string? PreferredEndsOn { get; set; }

    [BsonElement("ownerSlot")]
    public int OwnerSlot { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
