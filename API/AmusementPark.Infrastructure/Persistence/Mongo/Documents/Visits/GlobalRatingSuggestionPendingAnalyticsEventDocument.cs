using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class GlobalRatingSuggestionPendingAnalyticsEventDocument
{
    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("interactionType")]
    [BsonRepresentation(BsonType.String)]
    public GlobalRatingSuggestionInteractionType InteractionType { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}
