using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

/// <summary>
/// Paramètres Mongo Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSettingsDocument : MongoDocumentBase
{
    [BsonElement("source")]
    public string Source { get; set; } = "captain-coaster";

    [BsonElement("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [BsonElement("baseUrl")]
    public string BaseUrl { get; set; } = "https://captaincoaster.com/api";

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [BsonElement("dataDirectoryPath")]
    [BsonIgnoreIfNull]
    public string? DataDirectoryPath { get; set; }

    [BsonElement("htmlDirectoryPath")]
    [BsonIgnoreIfNull]
    public string? HtmlDirectoryPath { get; set; }

    [BsonElement("useOfflineMode")]
    public bool UseOfflineMode { get; set; }

    [BsonElement("lastSuccessfulSyncUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastSuccessfulSyncUtc { get; set; }

    [BsonElement("sitemapUrl")]
    [BsonIgnoreIfNull]
    public string? SitemapUrl { get; set; }

    [BsonElement("mapPageUrl")]
    [BsonIgnoreIfNull]
    public string? MapPageUrl { get; set; }

    [BsonElement("delayBetweenRequestsMs")]
    public int DelayBetweenRequestsMs { get; set; } = 1200;

    [BsonElement("httpTimeoutSeconds")]
    public int HttpTimeoutSeconds { get; set; } = 30;

    [BsonElement("maxRetryCount")]
    public int MaxRetryCount { get; set; } = 3;

    [BsonElement("maxConcurrentRequests")]
    public int MaxConcurrentRequests { get; set; } = 4;

    [BsonElement("coasterWriteBatchSize")]
    public int CoasterWriteBatchSize { get; set; } = 50;

    [BsonElement("progressSaveInterval")]
    public int ProgressSaveInterval { get; set; } = 25;

    [BsonElement("maxCoasterCount")]
    [BsonIgnoreIfNull]
    public int? MaxCoasterCount { get; set; }

    [BsonElement("skipCoasterCount")]
    public int SkipCoasterCount { get; set; }

    [BsonElement("enrichParkCoordinates")]
    public bool EnrichParkCoordinates { get; set; } = true;

    [BsonElement("mapMarkersAttributeName")]
    public string MapMarkersAttributeName { get; set; } = "data-map-markers-value";

    [BsonElement("coasterTitleXPath")]
    public string CoasterTitleXPath { get; set; } = "//h1";

    [BsonElement("characteristicsItemXPath")]
    public string CharacteristicsItemXPath { get; set; } = "//div[contains(@class,'list-group-item')]";

    [BsonElement("characteristicLabelXPath")]
    public string CharacteristicLabelXPath { get; set; } = ".//label";

    [BsonElement("characteristicValueXPath")]
    public string CharacteristicValueXPath { get; set; } = ".//div[contains(@class,'pull-right')]";

    [BsonElement("topMetricXPath")]
    public string TopMetricXPath { get; set; } = "//button[contains(@class,'btn-float-lg')]//div[contains(@class,'text-bold')]";
}

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

public sealed class CaptainCoasterDiscoveredUrlDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("captainCoasterId")]
    public string CaptainCoasterId { get; set; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; set; } = "fr";

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("sequence")]
    public int Sequence { get; set; }

    [BsonElement("discoveredAtUtc")]
    public DateTime DiscoveredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CaptainCoasterSyncLogEntryDocument
{
    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("level")]
    public string Level { get; set; } = "Info";

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class CaptainCoasterParkSnapshotDocument : MongoGeolocatedDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("captainCoasterId")]
    public string CaptainCoasterId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("countryRaw")]
    [BsonIgnoreIfNull]
    public string? CountryRaw { get; set; }

    [BsonElement("coasterCount")]
    public int CoasterCount { get; set; }

    [BsonElement("sampleCoasterNames")]
    public List<string> SampleCoasterNames { get; set; } = new List<string>();

    [BsonElement("scrapedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ScrapedAtUtc { get; set; }
}

public sealed class CaptainCoasterCoasterSnapshotDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("captainCoasterId")]
    public string CaptainCoasterId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("parkCaptainCoasterId")]
    [BsonIgnoreIfNull]
    public string? ParkCaptainCoasterId { get; set; }

    [BsonElement("parkName")]
    [BsonIgnoreIfNull]
    public string? ParkName { get; set; }

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("countryRaw")]
    [BsonIgnoreIfNull]
    public string? CountryRaw { get; set; }

    [BsonElement("manufacturer")]
    [BsonIgnoreIfNull]
    public string? Manufacturer { get; set; }

    [BsonElement("model")]
    [BsonIgnoreIfNull]
    public string? Model { get; set; }

    [BsonElement("materialType")]
    [BsonIgnoreIfNull]
    public string? MaterialType { get; set; }

    [BsonElement("seatingType")]
    [BsonIgnoreIfNull]
    public string? SeatingType { get; set; }

    [BsonElement("launchType")]
    [BsonIgnoreIfNull]
    public string? LaunchType { get; set; }

    [BsonElement("restraint")]
    [BsonIgnoreIfNull]
    public string? Restraint { get; set; }

    [BsonElement("isLaunched")]
    public bool IsLaunched { get; set; }

    [BsonElement("speedInKmH")]
    [BsonIgnoreIfNull]
    public double? SpeedInKmH { get; set; }

    [BsonElement("heightInMeters")]
    [BsonIgnoreIfNull]
    public double? HeightInMeters { get; set; }

    [BsonElement("lengthInMeters")]
    [BsonIgnoreIfNull]
    public double? LengthInMeters { get; set; }

    [BsonElement("dropInMeters")]
    [BsonIgnoreIfNull]
    public double? DropInMeters { get; set; }

    [BsonElement("inversionCount")]
    [BsonIgnoreIfNull]
    public int? InversionCount { get; set; }

    [BsonElement("status")]
    [BsonIgnoreIfNull]
    public string? Status { get; set; }

    [BsonElement("openingDate")]
    [BsonIgnoreIfNull]
    public DateTime? OpeningDate { get; set; }

    [BsonElement("closingDate")]
    [BsonIgnoreIfNull]
    public DateTime? ClosingDate { get; set; }

    [BsonElement("scrapedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ScrapedAtUtc { get; set; }
}

public sealed class CaptainCoasterComparisonResultDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("changeType")]
    public string ChangeType { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("localEntityId")]
    [BsonIgnoreIfNull]
    public string? LocalEntityId { get; set; }

    [BsonElement("externalEntityId")]
    [BsonIgnoreIfNull]
    public string? ExternalEntityId { get; set; }

    [BsonElement("matchConfidence")]
    public string MatchConfidence { get; set; } = "Unknown";

    [BsonElement("changes")]
    public List<CaptainCoasterFieldChangeDocument> Changes { get; set; } = new List<CaptainCoasterFieldChangeDocument>();

    [BsonElement("isApplied")]
    public bool IsApplied { get; set; }

    [BsonElement("hasExternalDuplicates")]
    public bool HasExternalDuplicates { get; set; }

    [BsonElement("requiresManualResolution")]
    public bool RequiresManualResolution { get; set; }

    [BsonElement("resolutionStatus")]
    public string ResolutionStatus { get; set; } = "NotRequired";

    [BsonElement("appliedExternalVariantId")]
    [BsonIgnoreIfNull]
    public string? AppliedExternalVariantId { get; set; }

    [BsonElement("externalVariants")]
    public List<CaptainCoasterExternalVariantOptionDocument> ExternalVariants { get; set; } = new List<CaptainCoasterExternalVariantOptionDocument>();
}

public sealed class CaptainCoasterExternalVariantOptionDocument
{
    [BsonElement("externalVariantId")]
    public string ExternalVariantId { get; set; } = string.Empty;

    [BsonElement("displayLabel")]
    public string DisplayLabel { get; set; } = string.Empty;

    [BsonElement("candidateLocalEntityId")]
    [BsonIgnoreIfNull]
    public string? CandidateLocalEntityId { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("isSuggested")]
    public bool IsSuggested { get; set; }

    [BsonElement("changes")]
    public List<CaptainCoasterFieldChangeDocument> Changes { get; set; } = new List<CaptainCoasterFieldChangeDocument>();
}

public sealed class CaptainCoasterFieldChangeDocument
{
    [BsonElement("field")]
    public string Field { get; set; } = string.Empty;

    [BsonElement("localValue")]
    [BsonIgnoreIfNull]
    public string? LocalValue { get; set; }

    [BsonElement("externalValue")]
    [BsonIgnoreIfNull]
    public string? ExternalValue { get; set; }

    [BsonElement("isDifferent")]
    public bool IsDifferent { get; set; }
}
