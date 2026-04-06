using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

/// <summary>
/// Paramètres Mongo Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSettingsDocument : MongoDocumentBase
{
    [BsonElement("source")]
    public string Source { get; set; } = "CaptainCoaster";

    [BsonElement("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [BsonElement("baseUrl")]
    public string BaseUrl { get; set; } = "https://captaincoaster.com/api";

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [BsonElement("dataDirectoryPath")]
    public string? DataDirectoryPath { get; set; }

    [BsonElement("htmlDirectoryPath")]
    public string? HtmlDirectoryPath { get; set; }

    [BsonElement("useOfflineMode")]
    public bool UseOfflineMode { get; set; }

    [BsonElement("lastSuccessfulSyncUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastSuccessfulSyncUtc { get; set; }
}

/// <summary>
/// Session Mongo d'import Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSyncSessionDocument : MongoDocumentBase
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
    [BsonIgnoreIfNull]
    public string? CurrentStep { get; set; }

    [BsonElement("message")]
    [BsonIgnoreIfNull]
    public string? Message { get; set; }
}
