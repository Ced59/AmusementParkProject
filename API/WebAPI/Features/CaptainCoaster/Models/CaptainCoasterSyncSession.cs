using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterSyncSession : ModelBase
    {
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

        [BsonElement("metrics")]
        public CaptainCoasterSyncMetrics Metrics { get; set; } = new CaptainCoasterSyncMetrics();

        [BsonElement("logs")]
        public List<CaptainCoasterSyncLogEntry> Logs { get; set; } = new List<CaptainCoasterSyncLogEntry>();
    }

    public sealed class CaptainCoasterSyncMetrics
    {
        [BsonElement("parksFetched")]
        public int ParksFetched { get; set; }

        [BsonElement("coastersFetched")]
        public int CoastersFetched { get; set; }

        [BsonElement("comparisonResults")]
        public int ComparisonResults { get; set; }

        [BsonElement("appliedChanges")]
        public int AppliedChanges { get; set; }
    }

    public sealed class CaptainCoasterSyncLogEntry
    {
        [BsonElement("occurredAtUtc")]
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("level")]
        public string Level { get; set; } = "Info";

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;
    }
}
