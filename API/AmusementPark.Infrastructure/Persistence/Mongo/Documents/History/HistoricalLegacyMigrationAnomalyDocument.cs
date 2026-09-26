using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoricalLegacyMigrationAnomalyDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("migrationId")]
    public string MigrationId { get; set; } = string.Empty;

    [BsonElement("legacyEventId")]
    public string LegacyEventId { get; set; } = string.Empty;

    [BsonElement("codes")]
    public List<string> Codes { get; set; } = new List<string>();

    [BsonElement("recordedAtUtc")]
    public DateTime RecordedAtUtc { get; set; }
}
