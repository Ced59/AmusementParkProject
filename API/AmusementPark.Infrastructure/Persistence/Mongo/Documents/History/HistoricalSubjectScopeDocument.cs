using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoricalSubjectScopeDocument : MongoDocumentBase
{
    [BsonElement("subjectType")]
    [BsonRepresentation(BsonType.String)]
    public HistoricalSubjectType SubjectType { get; set; }

    [BsonElement("subjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [BsonElement("contextParkId")]
    public string ContextParkId { get; set; } = string.Empty;

    public static HistoricalSubjectScopeDocument FromParkZone(
        ParkZoneDocument zone,
        DateTime retainedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(zone);
        string zoneId = Normalize(zone.Id, nameof(zone.Id));
        string parkId = Normalize(zone.ParkId, nameof(zone.ParkId));
        DateTime normalizedRetainedAtUtc = retainedAtUtc.Kind == DateTimeKind.Utc
            ? retainedAtUtc
            : retainedAtUtc.ToUniversalTime();

        return new HistoricalSubjectScopeDocument
        {
            Id = BuildId(HistoricalSubjectType.ParkZone, zoneId),
            SubjectType = HistoricalSubjectType.ParkZone,
            SubjectId = zoneId,
            ContextParkId = parkId,
            CreatedAt = normalizedRetainedAtUtc,
            UpdatedAt = normalizedRetainedAtUtc,
        };
    }

    public static string BuildId(HistoricalSubjectType subjectType, string subjectId)
    {
        return $"{subjectType}:{Normalize(subjectId, nameof(subjectId))}";
    }

    private static string Normalize(string value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A historical subject scope requires an identifier.", parameterName);
        }

        return normalizedValue;
    }
}
