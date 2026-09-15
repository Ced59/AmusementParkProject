using AmusementPark.Core.Domain.FactualEvents;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;

[BsonIgnoreExtraElements]
public sealed class ChangeTargetDocument
{
    [BsonElement("type")]
    public FactualTargetType Type { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("parentParkId")]
    [BsonIgnoreIfNull]
    public string? ParentParkId { get; set; }
}
