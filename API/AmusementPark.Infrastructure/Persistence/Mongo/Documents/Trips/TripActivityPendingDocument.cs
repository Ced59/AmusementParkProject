using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripActivityPendingDocument
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("actorMemberId")]
    [BsonIgnoreIfNull]
    public string? ActorMemberId { get; set; }

    [BsonElement("actorRole")]
    [BsonRepresentation(BsonType.String)]
    [BsonIgnoreIfNull]
    public TripEffectiveRole? ActorRole { get; set; }

    [BsonElement("kind")]
    [BsonRepresentation(BsonType.String)]
    public TripActivityKind Kind { get; set; }

    [BsonElement("operationKey")]
    public string OperationKey { get; set; } = string.Empty;

    [BsonElement("affectedCount")]
    public int AffectedCount { get; set; }

    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }

    public static TripActivityPendingDocument FromWrite(TripActivityWrite activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new TripActivityPendingDocument
        {
            TripPlanId = activity.TripPlanId.Value,
            ActorMemberId = activity.ActorMemberId?.Value,
            ActorRole = activity.ActorRole,
            Kind = activity.Kind,
            OperationKey = activity.OperationKey,
            AffectedCount = activity.AffectedCount,
            OccurredAtUtc = activity.OccurredAtUtc,
        };
    }

    public TripActivityWrite ToWrite()
    {
        return new TripActivityWrite(
            AmusementPark.Core.Domain.Trips.TripPlanId.Parse(this.TripPlanId),
            string.IsNullOrWhiteSpace(this.ActorMemberId)
                ? null
                : TripMemberId.Parse(this.ActorMemberId),
            this.ActorRole,
            this.Kind,
            this.OperationKey,
            this.AffectedCount,
            DateTime.SpecifyKind(this.OccurredAtUtc, DateTimeKind.Utc));
    }
}
