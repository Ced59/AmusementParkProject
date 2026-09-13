using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ProfileComparisonPassportResolver
{
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IPassportProfileShareSnapshotRepository snapshotRepository;
    private readonly ISharePublicationAccessResolver accessResolver;

    public ProfileComparisonPassportResolver(
        ISharePublicationRepository publicationRepository,
        IPassportProfileShareSnapshotRepository snapshotRepository,
        ISharePublicationAccessResolver accessResolver)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
        this.accessResolver = accessResolver
            ?? throw new ArgumentNullException(nameof(accessResolver));
    }

    public async Task<ProfileComparisonPassportReference?> ResolveCurrentAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string sourceScopeKey = PassportProfileShareSourceScope.Create(userId);
        SharePublication? publication = await this.publicationRepository.GetOwnedBySourceAsync(
            userId,
            SharePublicationType.PassportProfile,
            sourceScopeKey,
            cancellationToken);
        return await this.ResolveAsync(publication, cancellationToken);
    }

    public async Task<ProfileComparisonPassportReference?> ResolveExactAsync(
        SharePublicationId publicationId,
        string userId,
        long publicationVersion,
        CancellationToken cancellationToken)
    {
        SharePublication? publication = await this.publicationRepository.GetOwnedAsync(
            publicationId,
            userId,
            cancellationToken);
        if (publication?.PublicationVersion != publicationVersion)
        {
            return null;
        }

        return await this.ResolveAsync(publication, cancellationToken);
    }

    private async Task<ProfileComparisonPassportReference?> ResolveAsync(
        SharePublication? publication,
        CancellationToken cancellationToken)
    {
        if (publication?.IsResolvable != true
            || publication.Type != SharePublicationType.PassportProfile
            || !publication.ShareToken.HasValue)
        {
            return null;
        }

        ApplicationResult<ResolvedSharePublicationResult> access =
            await this.accessResolver.ResolveAsync(
                publication.ShareToken.Value.Value,
                SharePublicationType.PassportProfile,
                cancellationToken);
        if (!access.IsSuccess
            || access.Value is null
            || !Matches(publication, access.Value))
        {
            return null;
        }

        PassportProfileShareSnapshot? snapshot = await this.snapshotRepository.GetAsync(
            publication.Id,
            publication.PublicationVersion,
            cancellationToken);
        if (snapshot is null
            || !snapshot.Content.AllowsComparisons
            || snapshot.SourceVersion != publication.SourceVersion
            || snapshot.PolicySchemaVersion != publication.ContentPolicy.SchemaVersion
            || snapshot.DatePrecision != publication.ContentPolicy.DatePrecision
            || !snapshot.IncludedFields.SequenceEqual(publication.ContentPolicy.IncludedFields)
            || !string.Equals(
                snapshot.ContentFingerprint,
                publication.ContentFingerprint,
                StringComparison.Ordinal))
        {
            return null;
        }

        return new ProfileComparisonPassportReference(
            publication.Id,
            publication.PublicationVersion,
            snapshot.Content.DisplayName,
            publication.ContentPolicy,
            snapshot);
    }

    private static bool Matches(
        SharePublication publication,
        ResolvedSharePublicationResult access)
    {
        return string.Equals(access.PublicationId, publication.Id.Value, StringComparison.Ordinal)
            && string.Equals(access.OwnerUserId, publication.OwnerUserId, StringComparison.Ordinal)
            && access.PublicationType == publication.Type
            && string.Equals(
                access.SourceScopeKey,
                publication.SourceScopeKey,
                StringComparison.Ordinal)
            && access.SourceVersion == publication.SourceVersion
            && access.PublicationVersion == publication.PublicationVersion
            && access.PublishedAtUtc == publication.PublishedAtUtc
            && string.Equals(
                access.ContentFingerprint,
                publication.ContentFingerprint,
                StringComparison.Ordinal)
            && access.ContentPolicy.HasSameSelectionAs(publication.ContentPolicy);
    }
}
