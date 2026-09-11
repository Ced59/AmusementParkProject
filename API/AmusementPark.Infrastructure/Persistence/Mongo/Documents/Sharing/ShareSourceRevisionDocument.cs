using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

[BsonIgnoreExtraElements]
public sealed class ShareSourceRevisionDocument
{
    [BsonId]
    public string ScopeKey { get; set; } = string.Empty;

    [BsonElement("revision")]
    public long Revision { get; set; }

    [BsonElement("sourceFingerprint")]
    [BsonIgnoreIfNull]
    public string? SourceFingerprint { get; set; }

    [BsonElement("mutationLeases")]
    public List<ShareSourceMutationLeaseDocument> MutationLeases { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
