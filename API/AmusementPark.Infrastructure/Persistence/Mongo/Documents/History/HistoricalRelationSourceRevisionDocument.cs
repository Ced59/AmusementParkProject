using AmusementPark.Core.Domain.History;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalRelationSourceRevisionDocument
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("sourceSubject")]
    public HistoricalSubjectKeyDocument SourceSubject { get; set; } = new HistoricalSubjectKeyDocument();

    [BsonElement("targetSubject")]
    public HistoricalSubjectKeyDocument TargetSubject { get; set; } = new HistoricalSubjectKeyDocument();

    [BsonElement("relationType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalRelationType RelationType { get; set; }

    [BsonElement("period")]
    public HistoricalPeriodDocument Period { get; set; } = new HistoricalPeriodDocument();

    [BsonElement("position")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalEvidencePosition Position { get; set; }

    [BsonElement("scopes")]
    [BsonRepresentation(BsonType.String)]
    public List<HistoricalSourceScope> Scopes { get; set; } = new List<HistoricalSourceScope>();
}
