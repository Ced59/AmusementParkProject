using AmusementPark.Core.Domain.History;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalSubjectDocument
{
    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSubjectType Type { get; set; }

    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("historicalLabel")]
    public string HistoricalLabel { get; set; } = string.Empty;

    [BsonElement("publicationPolicy")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSubjectPublicationPolicy PublicationPolicy { get; set; }
}
