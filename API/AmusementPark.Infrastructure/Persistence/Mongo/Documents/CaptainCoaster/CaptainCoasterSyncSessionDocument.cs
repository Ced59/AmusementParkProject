using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

/// <summary>
/// Session Mongo d'import Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSyncSessionDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("status")]
    public string Status { get; set; } = "Pending";

    [BsonElement("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("progressPercentage")]
    public int ProgressPercentage { get; set; }

    [BsonElement("currentStep")]
    public string CurrentStep { get; set; } = string.Empty;

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("importKind")]
    public string ImportKind { get; set; } = string.Empty;

    [BsonElement("lastCompletedStep")]
    [BsonIgnoreIfNull]
    public string? LastCompletedStep { get; set; }

    [BsonElement("availableSteps")]
    public List<string> AvailableSteps { get; set; } = new List<string>();

    [BsonElement("canResume")]
    public bool CanResume { get; set; }

    [BsonElement("discoveredUrls")]
    [BsonIgnoreIfNull]
    public List<string>? DiscoveredUrls { get; set; }

    [BsonElement("metrics")]
    public CaptainCoasterSyncMetricsDocument Metrics { get; set; } = new CaptainCoasterSyncMetricsDocument();

    [BsonElement("logs")]
    public List<CaptainCoasterSyncLogEntryDocument> Logs { get; set; } = new List<CaptainCoasterSyncLogEntryDocument>();
}
