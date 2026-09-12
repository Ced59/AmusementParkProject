using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSource
    : ISharePublicationSourceDescriptor, IPassportProfileShareSourceVersionProvider
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;

    public PassportProfileSharePublicationSource(
        IShareSourceRevisionRepository sourceRevisionRepository)
    {
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
    }

    public SharePublicationType PublicationType => SharePublicationType.PassportProfile;

    public ApplicationResult<string> ResolveSourceScopeKey(string ownerUserId, string? sourceId)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0 || !string.IsNullOrWhiteSpace(sourceId))
        {
            return ApplicationResult<string>.Failure(SharingApplicationErrors.InvalidSource());
        }

        return ApplicationResult<string>.Success(
            PassportProfileShareSourceScope.Create(normalizedOwner));
    }

    public ShareContentPolicy CreateDefaultPolicy()
    {
        return ShareContentPolicy.Create(
            this.PublicationType,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.RideCount,
                ShareContentField.GeographicStatistics,
            });
    }

    public ApplicationResult<bool> ValidatePolicyForPublication(ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        bool includesUnsupportedContent = contentPolicy.IncludedFields.Any(
            static field => field is not ShareContentField.PublicDisplayName
                and not ShareContentField.Avatar
                and not ShareContentField.RideCount
                and not ShareContentField.TemporalRatings
                and not ShareContentField.GlobalRatings
                and not ShareContentField.PublicCaption
                and not ShareContentField.GeographicStatistics
                and not ShareContentField.MissedItems);
        return contentPolicy.PublicationType == this.PublicationType
            && contentPolicy.DatePrecision == ShareDatePrecision.Year
            && !includesUnsupportedContent
            ? ApplicationResult<bool>.Success(true)
            : ApplicationResult<bool>.Failure(
                SharingApplicationErrors.PublicContentNotSupported());
    }

    public async Task<ApplicationResult<long>> GetCurrentSourceVersionAsync(
        string sourceScopeKey,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        if (!PassportProfileShareSourceScope.TryParse(sourceScopeKey, out string ownerUserId))
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            ownerUserId,
            cancellationToken);
        return CreateVersion(
            snapshot,
            contentPolicy.Includes(ShareContentField.GlobalRatings));
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        PrepareOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            bool includeRatings,
            CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0)
        {
            return ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        await this.sourceRevisionRepository.GetOrCreateAsync(
            PassportProfileShareSourceScope.Create(normalizedOwnerUserId),
            cancellationToken);
        return await this.GetOwnedSourceRevisionSnapshotAsync(
            normalizedOwnerUserId,
            includeRatings,
            cancellationToken);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        GetOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            bool includeRatings,
            CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0)
        {
            return ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            normalizedOwnerUserId,
            cancellationToken);
        return snapshot.IsStableFor(includeRatings)
            ? ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(snapshot)
            : ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevision>>
        ReconcileOwnedSourceVersionAsync(
        string ownerUserId,
        string sourceFingerprint,
        PassportProfileShareSourceRevisionSnapshot expectedSnapshot,
        bool includeRatings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedSnapshot);
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0 || !expectedSnapshot.IsStableFor(includeRatings))
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ShareSourceRevision passportRevision =
            await this.sourceRevisionRepository.ReconcileFingerprintAsync(
                PassportProfileShareSourceScope.Create(normalizedOwnerUserId),
                sourceFingerprint,
                cancellationToken);
        PassportProfileShareSourceRevisionSnapshot snapshotAfter = await this.ReadSnapshotAsync(
            normalizedOwnerUserId,
            cancellationToken);
        bool passportRevisionIsExpected =
            passportRevision.Revision == expectedSnapshot.Passport.Revision
            || (expectedSnapshot.Passport.Revision < long.MaxValue
                && passportRevision.Revision == expectedSnapshot.Passport.Revision + 1);
        if (!passportRevision.IsStable
            || !snapshotAfter.IsStableFor(includeRatings)
            || !passportRevisionIsExpected
            || snapshotAfter.Passport.Revision != passportRevision.Revision
            || snapshotAfter.Identity.Revision != expectedSnapshot.Identity.Revision
            || (includeRatings
                && snapshotAfter.Ratings.Revision != expectedSnapshot.Ratings.Revision)
            || snapshotAfter.Catalog.Revision != expectedSnapshot.Catalog.Revision)
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ApplicationResult<long> versionResult = CreateVersion(snapshotAfter, includeRatings);
        return versionResult.IsSuccess
            ? ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(
                    versionResult.Value,
                    sourceFingerprint))
            : ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                versionResult.Errors);
    }

    private async Task<PassportProfileShareSourceRevisionSnapshot> ReadSnapshotAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string identityScope = PublicIdentityShareSourceScope.Create(ownerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[]
                {
                    passportScope,
                    identityScope,
                    ratingsScope,
                    PersonalRankingShareSourceScope.PublicCatalog,
                },
                cancellationToken);
        return new PassportProfileShareSourceRevisionSnapshot(
            revisions[passportScope],
            revisions[identityScope],
            revisions[ratingsScope],
            revisions[PersonalRankingShareSourceScope.PublicCatalog]);
    }

    private static ApplicationResult<long> CreateVersion(
        PassportProfileShareSourceRevisionSnapshot snapshot,
        bool includeRatings)
    {
        if (!snapshot.IsStableFor(includeRatings))
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            return ApplicationResult<long>.Success(checked(
                snapshot.Passport.Revision
                + snapshot.Identity.Revision
                + (includeRatings ? snapshot.Ratings.Revision : 0)
                + snapshot.Catalog.Revision));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }
}
