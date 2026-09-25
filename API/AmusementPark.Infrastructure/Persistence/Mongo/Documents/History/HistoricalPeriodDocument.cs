using AmusementPark.Core.Domain.History;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalPeriodDocument
{
    [BsonElement("start")]
    [BsonIgnoreIfNull]
    public HistoricalDateDocument? Start { get; set; }

    [BsonElement("end")]
    [BsonIgnoreIfNull]
    public HistoricalDateDocument? End { get; set; }

    [BsonElement("startConfidence")]
    [BsonRepresentation(BsonType.String)]
    public PeriodBoundaryConfidence StartConfidence { get; set; }

    [BsonElement("endConfidence")]
    [BsonRepresentation(BsonType.String)]
    public PeriodBoundaryConfidence EndConfidence { get; set; }
}
