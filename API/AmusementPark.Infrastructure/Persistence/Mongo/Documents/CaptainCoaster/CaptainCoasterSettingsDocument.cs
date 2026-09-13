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
