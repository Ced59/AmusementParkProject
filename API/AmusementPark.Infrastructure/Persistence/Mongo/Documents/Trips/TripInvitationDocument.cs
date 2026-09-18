using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

[BsonIgnoreExtraElements]
public sealed class TripInvitationDocument : MongoDocumentBase
{
    [BsonElement("tripPlanId")]
    public string TripPlanId { get; set; } = string.Empty;

    [BsonElement("tripTitle")]
    public string TripTitle { get; set; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; set; } = string.Empty;

    [BsonElement("tokenHint")]
    public string TokenHint { get; set; } = string.Empty;

    [BsonElement("proposedRole")]
    [BsonRepresentation(BsonType.String)]
    public TripDelegatedRole ProposedRole { get; set; }

    [BsonElement("inviterMemberId")]
    public string InviterMemberId { get; set; } = string.Empty;

    [BsonElement("inviterDisplayName")]
    public string InviterDisplayName { get; set; } = string.Empty;

    [BsonElement("targetEmailHmac")]
    [BsonIgnoreIfNull]
    public string? TargetEmailHmac { get; set; }

    [BsonElement("targetEmailHmacKeyVersion")]
    [BsonIgnoreIfNull]
    public string? TargetEmailHmacKeyVersion { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public TripInvitationStatus Status { get; set; }

    [BsonElement("previewPolicy")]
    [BsonRepresentation(BsonType.String)]
    public TripInvitationPreviewPolicy PreviewPolicy { get; set; }

    [BsonElement("periodKind")]
    [BsonRepresentation(BsonType.String)]
    public TripInvitationPeriodKind PeriodKind { get; set; }

    [BsonElement("startMonth")]
    [BsonIgnoreIfNull]
    public string? StartMonth { get; set; }

    [BsonElement("endMonth")]
    [BsonIgnoreIfNull]
    public string? EndMonth { get; set; }

    [BsonElement("memberCountBand")]
    [BsonRepresentation(BsonType.String)]
    public TripInvitationMemberCountBand MemberCountBand { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("revokedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("operationKeyHash")]
    public string OperationKeyHash { get; set; } = string.Empty;

    [BsonElement("requestHash")]
    public string RequestHash { get; set; } = string.Empty;

    [BsonElement("sealedToken")]
    [BsonIgnoreIfNull]
    public string? SealedToken { get; set; }

    [BsonElement("sealedTokenKeyVersion")]
    [BsonIgnoreIfNull]
    public string? SealedTokenKeyVersion { get; set; }

    [BsonElement("childMutationEpoch")]
    public long ChildMutationEpoch { get; set; }

    [BsonElement("leaseGeneration")]
    public long LeaseGeneration { get; set; }

    [BsonElement("leaseExpiresAtUtc")]
    public DateTime LeaseExpiresAtUtc { get; set; }

    [BsonElement("activeSlot")]
    [BsonIgnoreIfNull]
    public int? ActiveSlot { get; set; }

    [BsonElement("reservedExpiresAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ReservedExpiresAtUtc { get; set; }
}
