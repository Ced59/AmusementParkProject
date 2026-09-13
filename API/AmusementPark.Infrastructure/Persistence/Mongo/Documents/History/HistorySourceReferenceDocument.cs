using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistorySourceReferenceDocument
{
    [BsonElement("label")]
    [BsonIgnoreIfNull]
    public string? Label { get; set; }

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("accessedAt")]
    [BsonIgnoreIfNull]
    public string? AccessedAt { get; set; }
}
