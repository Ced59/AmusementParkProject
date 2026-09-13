using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

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
