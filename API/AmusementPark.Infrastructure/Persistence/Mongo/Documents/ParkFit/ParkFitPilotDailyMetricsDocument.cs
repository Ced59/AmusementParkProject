using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitPilotDailyMetricsDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("dateUtc")]
    public DateTime DateUtc { get; set; }

    [BsonElement("eventCounts")]
    public Dictionary<string, long> EventCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("resultBandCounts")]
    public Dictionary<string, long> ResultBandCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("unknownLevelCounts")]
    public Dictionary<string, long> UnknownLevelCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("durationBandCounts")]
    public Dictionary<string, long> DurationBandCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("failureKindCounts")]
    public Dictionary<string, long> FailureKindCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("comparisonSizeCounts")]
    public Dictionary<string, long> ComparisonSizeCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("qualityIssueCounts")]
    public Dictionary<string, long> QualityIssueCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("zeroResultQualityIssueCounts")]
    public Dictionary<string, long> ZeroResultQualityIssueCounts { get; set; } =
        new(StringComparer.Ordinal);

    [BsonElement("methodVersionCounts")]
    public Dictionary<string, long> MethodVersionCounts { get; set; } = new(StringComparer.Ordinal);

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}
