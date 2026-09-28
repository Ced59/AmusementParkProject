using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;

[BsonIgnoreExtraElements]
public sealed class ExternalLiveTargetMappingDocument : MongoDocumentBase
{
    [BsonElement("mappingId")]
    public string MappingId { get; set; } = string.Empty;

    [BsonElement("sourceId")]
    public string SourceId { get; set; } = string.Empty;

    [BsonElement("externalTarget")]
    public ExternalLiveTargetDescriptorDocument ExternalTarget { get; set; } =
        new ExternalLiveTargetDescriptorDocument();

    [BsonElement("target")]
    [BsonIgnoreIfNull]
    public LiveTargetReferenceDocument? Target { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public LiveMappingStatus Status { get; set; }

    [BsonElement("confidence")]
    [BsonRepresentation(BsonType.String)]
    public LiveMappingConfidence Confidence { get; set; }

    [BsonElement("validFromUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ValidFromUtc { get; set; }

    [BsonElement("validToUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? ValidToUtc { get; set; }

    [BsonElement("revision")]
    public int Revision { get; set; }

    [BsonElement("supersedesRevision")]
    [BsonIgnoreIfNull]
    public int? SupersedesRevision { get; set; }

    [BsonElement("reviewedByUserId")]
    [BsonIgnoreIfNull]
    public string? ReviewedByUserId { get; set; }

    [BsonElement("reviewNote")]
    [BsonIgnoreIfNull]
    public string? ReviewNote { get; set; }

    [BsonElement("recordedAtUtc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime RecordedAtUtc { get; set; }
}
