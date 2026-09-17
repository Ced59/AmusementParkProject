using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

[BsonIgnoreExtraElements]
public sealed class NotificationDigestDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("channel")]
    public NotificationChannel Channel { get; set; }

    [BsonElement("frequency")]
    public NotificationFrequency Frequency { get; set; }

    [BsonElement("periodStart")]
    public DateTime PeriodStart { get; set; }

    [BsonElement("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    [BsonElement("entries")]
    public List<NotificationDigestEntryDocument> Entries { get; set; } = new();

    [BsonElement("observedNotificationCount")]
    public int ObservedNotificationCount { get; set; }
}
