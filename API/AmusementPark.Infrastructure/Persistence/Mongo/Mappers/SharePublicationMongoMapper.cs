using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class SharePublicationMongoMapper
{
    public static SharePublicationDocument ToDocument(this SharePublication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);
        return new SharePublicationDocument
        {
            Id = publication.Id.Value,
            OwnerUserId = publication.OwnerUserId,
            Type = publication.Type,
            SourceScopeKey = publication.SourceScopeKey,
            ShareToken = publication.ShareToken?.Value,
            Status = publication.Status,
            Visibility = publication.Visibility,
            ContentPolicy = new ShareContentPolicyDocument
            {
                SchemaVersion = publication.ContentPolicy.SchemaVersion,
                DatePrecision = publication.ContentPolicy.DatePrecision,
                IncludedFields = publication.ContentPolicy.IncludedFields.ToList(),
            },
            SourceVersion = publication.SourceVersion,
            PublicationVersion = publication.PublicationVersion,
            Version = publication.Version,
            PublishedAtUtc = publication.PublishedAtUtc,
            RevokedAtUtc = publication.RevokedAtUtc,
            CreatedAt = publication.CreatedAtUtc,
            UpdatedAt = publication.UpdatedAtUtc,
        };
    }

    public static SharePublication ToDomain(this SharePublicationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.ContentPolicy);
        return SharePublication.Restore(
            SharePublicationId.Parse(document.Id),
            document.OwnerUserId,
            document.Type,
            document.SourceScopeKey,
            document.ShareToken is null ? null : ShareToken.Parse(document.ShareToken),
            document.Status,
            document.Visibility,
            ShareContentPolicy.Restore(
                document.Type,
                document.ContentPolicy.SchemaVersion,
                document.ContentPolicy.DatePrecision,
                document.ContentPolicy.IncludedFields),
            document.SourceVersion,
            document.PublicationVersion,
            document.Version,
            document.PublishedAtUtc,
            document.RevokedAtUtc,
            document.CreatedAt,
            document.UpdatedAt);
    }
}
