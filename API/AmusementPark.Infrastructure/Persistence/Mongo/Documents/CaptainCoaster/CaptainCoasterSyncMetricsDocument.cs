using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterSyncMetricsDocument
{
    [BsonElement("parksFetched")]
    public int ParksFetched { get; set; }

    [BsonElement("coastersFetched")]
    public int CoastersFetched { get; set; }

    [BsonElement("comparisonResults")]
    public int ComparisonResults { get; set; }

    [BsonElement("appliedChanges")]
    public int AppliedChanges { get; set; }

    [BsonElement("duplicateConflicts")]
    public int DuplicateConflicts { get; set; }

    [BsonElement("discoveredItems")]
    public int DiscoveredItems { get; set; }

    [BsonElement("processedItems")]
    public int ProcessedItems { get; set; }

    [BsonElement("failedItems")]
    public int FailedItems { get; set; }

    [BsonElement("skippedItems")]
    public int SkippedItems { get; set; }
}
