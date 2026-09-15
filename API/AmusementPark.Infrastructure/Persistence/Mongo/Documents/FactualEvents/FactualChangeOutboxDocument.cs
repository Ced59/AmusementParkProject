using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;

[BsonIgnoreExtraElements]
public sealed class FactualChangeOutboxDocument : MongoDocumentBase
{
    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("type")]
    public FactualEventType Type { get; set; }

    [BsonElement("definitionVersion")]
    public int DefinitionVersion { get; set; }

    [BsonElement("target")]
    public ChangeTargetDocument Target { get; set; } = new ChangeTargetDocument();

    [BsonElement("previousValue")]
    [BsonIgnoreIfNull]
    public FactValueDocument? PreviousValue { get; set; }

    [BsonElement("newValue")]
    [BsonIgnoreIfNull]
    public FactValueDocument? NewValue { get; set; }

    [BsonElement("source")]
    public SourceReferenceDocument Source { get; set; } = new SourceReferenceDocument();

    [BsonElement("confidence")]
    public DataConfidence Confidence { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }

    [BsonElement("deduplicationKey")]
    public string DeduplicationKey { get; set; } = string.Empty;

    [BsonElement("sourceRevision")]
    public long SourceRevision { get; set; }

    [BsonElement("materializedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? MaterializedAtUtc { get; set; }

    [BsonElement("terminalAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? TerminalAtUtc { get; set; }

    [BsonElement("terminalErrorCode")]
    [BsonIgnoreIfNull]
    public string? TerminalErrorCode { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
