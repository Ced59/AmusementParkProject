using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripInvitationMongoMapper
{
    public static TripInvitationDocument ToDocument(this TripInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        return new TripInvitationDocument
        {
            Id = invitation.Id.Value,
            TripPlanId = invitation.TripPlanId.Value,
            TripTitle = invitation.TripTitle,
            TokenHash = invitation.TokenHash,
            TokenHint = invitation.TokenHint,
            ProposedRole = invitation.ProposedRole,
            InviterMemberId = invitation.InviterMemberId.Value,
            InviterDisplayName = invitation.InviterDisplayName,
            TargetEmailHmac = invitation.TargetEmailHmac,
            TargetEmailHmacKeyVersion = invitation.TargetEmailHmacKeyVersion,
            Status = invitation.Status,
            PreviewPolicy = invitation.PreviewPolicy,
            PeriodKind = invitation.PeriodPreview.Kind,
            StartMonth = invitation.PeriodPreview.StartMonth,
            EndMonth = invitation.PeriodPreview.EndMonth,
            MemberCountBand = invitation.MemberCountBand,
            ExpiresAtUtc = ToMongoPrecision(invitation.ExpiresAtUtc),
            RetentionExpiresAtUtc = ToMongoPrecision(
                invitation.ExpiresAtUtc.Add(TripInvitation.IdempotencyReplayRetention)),
            RevokedAtUtc = invitation.RevokedAtUtc.HasValue
                ? ToMongoPrecision(invitation.RevokedAtUtc.Value)
                : null,
            AcceptedAtUtc = invitation.AcceptedAtUtc.HasValue
                ? ToMongoPrecision(invitation.AcceptedAtUtc.Value)
                : null,
            DeclinedAtUtc = invitation.DeclinedAtUtc.HasValue
                ? ToMongoPrecision(invitation.DeclinedAtUtc.Value)
                : null,
            UseCount = invitation.UseCount,
            AcceptingUserId = invitation.AcceptingUserId,
            AcceptanceOperationId = invitation.AcceptanceOperationId,
            AcceptanceGeneration = invitation.AcceptanceGeneration,
            AcceptanceLeaseExpiresAtUtc = invitation.AcceptanceLeaseExpiresAtUtc.HasValue
                ? ToMongoPrecision(invitation.AcceptanceLeaseExpiresAtUtc.Value)
                : null,
            CreatedAt = ToMongoPrecision(invitation.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(invitation.UpdatedAtUtc),
            Version = invitation.Version,
        };
    }

    public static TripInvitation ToDomain(this TripInvitationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripInvitation.Restore(
            TripInvitationId.Parse(document.Id),
            TripPlanId.Parse(document.TripPlanId),
            document.TripTitle,
            document.TokenHash,
            document.TokenHint,
            document.ProposedRole,
            TripMemberId.Parse(document.InviterMemberId),
            document.InviterDisplayName,
            document.TargetEmailHmac,
            document.TargetEmailHmacKeyVersion,
            document.Status,
            document.PreviewPolicy,
            TripInvitationPeriodPreview.Restore(
                document.PeriodKind,
                document.StartMonth,
                document.EndMonth),
            document.MemberCountBand,
            AsUtc(document.ExpiresAtUtc),
            document.RevokedAtUtc.HasValue ? AsUtc(document.RevokedAtUtc.Value) : null,
            document.AcceptedAtUtc.HasValue ? AsUtc(document.AcceptedAtUtc.Value) : null,
            document.DeclinedAtUtc.HasValue ? AsUtc(document.DeclinedAtUtc.Value) : null,
            document.UseCount,
            document.AcceptingUserId,
            document.AcceptanceOperationId,
            document.AcceptanceGeneration,
            document.AcceptanceLeaseExpiresAtUtc.HasValue
                ? AsUtc(document.AcceptanceLeaseExpiresAtUtc.Value)
                : null,
            AsUtc(document.CreatedAt),
            AsUtc(document.UpdatedAt),
            document.Version);
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
