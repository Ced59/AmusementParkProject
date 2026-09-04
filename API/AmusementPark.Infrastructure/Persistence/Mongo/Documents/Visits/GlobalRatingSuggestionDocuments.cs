using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class GlobalRatingSuggestionStateDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public RatingTargetType TargetType { get; set; }

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("lastPresentedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastPresentedAtUtc { get; set; }

    [BsonElement("lastAcceptedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastAcceptedAtUtc { get; set; }

    [BsonElement("lastDismissedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastDismissedAtUtc { get; set; }

    [BsonElement("isAwaitingResolution")]
    [BsonIgnoreIfDefault]
    public bool IsAwaitingResolution { get; set; }

    [BsonElement("pendingAnalyticsEvents")]
    [BsonIgnoreIfDefault]
    public List<GlobalRatingSuggestionPendingAnalyticsEventDocument> PendingAnalyticsEvents { get; set; } =
        new List<GlobalRatingSuggestionPendingAnalyticsEventDocument>();
}

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

[BsonIgnoreExtraElements]
public sealed class GlobalRatingSuggestionPreferenceDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Événement analytique minimisé : aucune cible ni valeur exacte de note n'est conservée.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class GlobalRatingSuggestionInteractionDocument : MongoDocumentBase
{
    [BsonElement("userCohortKey")]
    public string UserCohortKey { get; set; } = string.Empty;

    [BsonElement("targetType")]
    [BsonRepresentation(BsonType.String)]
    public RatingTargetType TargetType { get; set; }

    [BsonElement("interactionType")]
    [BsonRepresentation(BsonType.String)]
    public GlobalRatingSuggestionInteractionType InteractionType { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }
}
