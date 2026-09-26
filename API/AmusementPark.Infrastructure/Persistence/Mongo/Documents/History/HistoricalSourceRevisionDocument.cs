using AmusementPark.Core.Domain.History;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalSourceRevisionDocument
{
    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("subjectType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSubjectType SubjectType { get; set; }

    [BsonElement("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [BsonElement("factType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalFactType FactType { get; set; }

    [BsonElement("period")]
    public HistoricalPeriodDocument Period { get; set; } = new HistoricalPeriodDocument();

    [BsonElement("position")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalEvidencePosition Position { get; set; }

    [BsonElement("scopes")]
    public List<HistoricalSourceScope> Scopes { get; set; } = new List<HistoricalSourceScope>();

    [BsonElement("historicalLabel")]
    [BsonIgnoreIfNull]
    public string? HistoricalLabel { get; set; }

    [BsonElement("structuredValue")]
    [BsonIgnoreIfNull]
    public string? StructuredValue { get; set; }

    [BsonElement("sequenceWithinDate")]
    [BsonIgnoreIfNull]
    public int? SequenceWithinDate { get; set; }

    [BsonElement("narrativeContentId")]
    [BsonIgnoreIfNull]
    public string? NarrativeContentId { get; set; }
}
