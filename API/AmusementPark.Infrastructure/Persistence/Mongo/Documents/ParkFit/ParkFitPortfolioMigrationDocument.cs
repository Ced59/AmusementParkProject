using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

[BsonIgnoreExtraElements]
public sealed class ParkFitPortfolioMigrationDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }
}
