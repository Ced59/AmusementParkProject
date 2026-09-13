using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ProfileComparisonInvitationMongoMapper
{
    private static readonly TimeSpan PendingRetention = TimeSpan.FromDays(7);

    public static ProfileComparisonInvitationDocument ToDocument(
        this ProfileComparisonInvitation invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);
        return new ProfileComparisonInvitationDocument
        {
            Id = invitation.Id.Value,
            Token = invitation.Token.Value,
            CreatorUserId = invitation.CreatorUserId,
            CreatorPassportPublicationId = invitation.CreatorPassportPublicationId.Value,
            CreatorPassportPublicationVersion = invitation.CreatorPassportPublicationVersion,
            Categories = invitation.Categories.ToList(),
            Status = invitation.Status,
            AcceptorUserId = invitation.AcceptorUserId,
            AcceptorPassportPublicationId = invitation.AcceptorPassportPublicationId?.Value,
            AcceptorPassportPublicationVersion = invitation.AcceptorPassportPublicationVersion,
            ComparisonId = invitation.ComparisonId?.Value,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            AcceptedAtUtc = invitation.AcceptedAtUtc,
            PurgeAtUtc = invitation.IsAccepted
                ? null
                : invitation.ExpiresAtUtc.Add(PendingRetention),
            Version = invitation.Version,
            CreatedAt = invitation.CreatedAtUtc,
            UpdatedAt = invitation.UpdatedAtUtc,
        };
    }

    public static ProfileComparisonInvitation ToDomain(
        this ProfileComparisonInvitationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ProfileComparisonInvitation.Restore(
            ProfileComparisonInvitationId.Parse(document.Id),
            ShareToken.Parse(document.Token),
            document.CreatorUserId,
            SharePublicationId.Parse(document.CreatorPassportPublicationId),
            document.CreatorPassportPublicationVersion,
            document.Categories,
            document.Status,
            document.AcceptorUserId,
            document.AcceptorPassportPublicationId is null
                ? null
                : SharePublicationId.Parse(document.AcceptorPassportPublicationId),
            document.AcceptorPassportPublicationVersion,
            document.ComparisonId is null
                ? null
                : ProfileComparisonId.Parse(document.ComparisonId),
            document.ExpiresAtUtc,
            document.AcceptedAtUtc,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }
}
