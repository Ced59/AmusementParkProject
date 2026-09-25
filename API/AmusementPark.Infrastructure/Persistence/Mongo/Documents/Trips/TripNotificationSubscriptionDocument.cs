using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripNotificationSubscriptionDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    [BsonElement("seenThroughSequence")]
    public long SeenThroughSequence { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
