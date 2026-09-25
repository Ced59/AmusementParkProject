using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalSourceRevisionDocument
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }
}
