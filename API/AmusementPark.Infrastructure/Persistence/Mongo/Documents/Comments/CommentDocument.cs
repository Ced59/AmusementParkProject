using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;

[BsonIgnoreExtraElements]
public sealed class CommentDocument : MongoDocumentBase
{
    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public CommentTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    public string ParkId { get; set; } = string.Empty;

    [BsonElement("authorUserId")]
    public string AuthorUserId { get; set; } = string.Empty;

    [BsonElement("authorPublicDisplayName")]
    public string AuthorDisplayName { get; set; } = string.Empty;

    [BsonElement("authorAvatarUrl")]
    [BsonIgnoreIfNull]
    public string? AuthorAvatarUrl { get; set; }

    [BsonElement("authorRole")]
    [BsonRepresentation(BsonType.String)]
    public Role AuthorRole { get; set; }

    [BsonElement("bodies")]
    public List<LocalizedTextDocument> Bodies { get; set; } = new List<LocalizedTextDocument>();

    [BsonElement("imageIds")]
    public List<string> ImageIds { get; set; } = new List<string>();

    [BsonElement("revision")]
    public long Revision { get; set; }

    [BsonElement("isOfficial")]
    public bool IsOfficial { get; set; }

    [BsonElement("moderationStatus")]
    [BsonRepresentation(BsonType.String)]
    public CommentModerationStatus ModerationStatus { get; set; }
}
