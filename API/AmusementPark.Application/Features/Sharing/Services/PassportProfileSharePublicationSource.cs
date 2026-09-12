using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSource
    : ISharePublicationSourceDescriptor, IPassportProfileShareSourceVersionProvider
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly IPassportProfileShareSnapshotRepository snapshotRepository;

    public PassportProfileSharePublicationSource(
        IShareSourceRevisionRepository sourceRevisionRepository,
        IPassportProfileShareSnapshotRepository snapshotRepository)
    {
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
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
        SharePublicationSourceVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ContentPolicy);
        if (!PassportProfileShareSourceScope.TryParse(
                request.SourceScopeKey,
                out string ownerUserId))
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        ApplicationResult<IReadOnlyCollection<string>> selectedParkIdsResult =
            await this.ResolveSelectedParkIdsAsync(request, cancellationToken);
        if (!selectedParkIdsResult.IsSuccess || selectedParkIdsResult.Value is null)
        {
            return ApplicationResult<long>.Failure(selectedParkIdsResult.Errors);
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            ownerUserId,
            request.ContentPolicy,
            selectedParkIdsResult.Value,
            cancellationToken);
        return CreateVersion(snapshot, request.ContentPolicy);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        PrepareOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            IReadOnlyCollection<string> selectedParkIds,
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
            contentPolicy,
            selectedParkIds,
            cancellationToken);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        GetOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            IReadOnlyCollection<string> selectedParkIds,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        string[] normalizedParkIds = NormalizeSelectedParkIds(selectedParkIds);
        if (normalizedOwnerUserId.Length == 0 || normalizedParkIds.Length == 0)
        {
            return ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            normalizedOwnerUserId,
            contentPolicy,
            normalizedParkIds,
            cancellationToken);
        return snapshot.IsStableFor(contentPolicy)
            ? ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Success(snapshot)
            : ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevision>>
        ReconcileOwnedSourceVersionAsync(
            string ownerUserId,
            string sourceFingerprint,
            PassportProfileShareSourceRevisionSnapshot expectedSnapshot,
            ShareContentPolicy contentPolicy,
            IReadOnlyCollection<string> selectedParkIds,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedSnapshot);
        ArgumentNullException.ThrowIfNull(contentPolicy);
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        string[] normalizedParkIds = NormalizeSelectedParkIds(selectedParkIds);
        if (normalizedOwnerUserId.Length == 0
            || normalizedParkIds.Length == 0
            || !expectedSnapshot.IsStableFor(contentPolicy))
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
            contentPolicy,
            normalizedParkIds,
            cancellationToken);
        bool passportRevisionIsExpected =
            passportRevision.Revision == expectedSnapshot.Passport.Revision
            || (expectedSnapshot.Passport.Revision < long.MaxValue
                && passportRevision.Revision == expectedSnapshot.Passport.Revision + 1);
        PassportProfileShareSourceRevisionSnapshot comparableExpected = expectedSnapshot with
        {
            Passport = snapshotAfter.Passport,
        };
        if (!passportRevision.IsStable
            || !snapshotAfter.IsStableFor(contentPolicy)
            || !passportRevisionIsExpected
            || snapshotAfter.Passport.Revision != passportRevision.Revision
            || !comparableExpected.HasSameRevisionsAs(snapshotAfter, contentPolicy))
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ApplicationResult<long> versionResult = CreateVersion(snapshotAfter, contentPolicy);
        return versionResult.IsSuccess
            ? ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(
                    versionResult.Value,
                    sourceFingerprint))
            : ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                versionResult.Errors);
    }

    private async Task<ApplicationResult<IReadOnlyCollection<string>>> ResolveSelectedParkIdsAsync(
        SharePublicationSourceVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SelectedParkIds is not null)
        {
            string[] selectedParkIds = NormalizeSelectedParkIds(request.SelectedParkIds);
            return selectedParkIds.Length == 0
                ? ApplicationResult<IReadOnlyCollection<string>>.Failure(
                    SharingApplicationErrors.SourceUnavailable())
                : ApplicationResult<IReadOnlyCollection<string>>.Success(selectedParkIds);
        }

        if (request.PublicationId is null
            || !request.PublicationVersion.HasValue
            || request.PublicationVersion.Value <= 0)
        {
            return ApplicationResult<IReadOnlyCollection<string>>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSnapshot? snapshot = await this.snapshotRepository.GetAsync(
            request.PublicationId.Value,
            request.PublicationVersion.Value,
            cancellationToken);
        string[] persistedParkIds = NormalizeSelectedParkIds(
            snapshot?.Selection.SelectedParkIds ?? Array.Empty<string>());
        return persistedParkIds.Length == 0
            ? ApplicationResult<IReadOnlyCollection<string>>.Failure(
                SharingApplicationErrors.SourceUnavailable())
            : ApplicationResult<IReadOnlyCollection<string>>.Success(persistedParkIds);
    }

    private async Task<PassportProfileShareSourceRevisionSnapshot> ReadSnapshotAsync(
        string ownerUserId,
        ShareContentPolicy contentPolicy,
        IReadOnlyCollection<string> selectedParkIds,
        CancellationToken cancellationToken)
    {
        string passportScope = PassportProfileShareSourceScope.Create(ownerUserId);
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(ownerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(ownerUserId);
        string ratingsScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        string[] catalogScopes = NormalizeSelectedParkIds(selectedParkIds)
            .Select(PublicCatalogShareSourceScope.CreatePark)
            .ToArray();
        List<string> scopes = new List<string> { passportScope };
        if (contentPolicy.Includes(ShareContentField.PublicDisplayName))
        {
            scopes.Add(displayNameScope);
        }

        if (contentPolicy.Includes(ShareContentField.Avatar))
        {
            scopes.Add(avatarScope);
        }

        if (contentPolicy.Includes(ShareContentField.GlobalRatings))
        {
            scopes.Add(ratingsScope);
        }

        scopes.AddRange(catalogScopes);
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(scopes, cancellationToken);
        Dictionary<string, ShareSourceRevision> catalog = catalogScopes.ToDictionary(
            static scope => scope,
            scope => revisions[scope],
            StringComparer.Ordinal);
        return new PassportProfileShareSourceRevisionSnapshot(
            revisions[passportScope],
            ResolveOptionalRevision(revisions, displayNameScope),
            ResolveOptionalRevision(revisions, avatarScope),
            ResolveOptionalRevision(revisions, ratingsScope),
            catalog);
    }

    private static ApplicationResult<long> CreateVersion(
        PassportProfileShareSourceRevisionSnapshot snapshot,
        ShareContentPolicy contentPolicy)
    {
        if (!snapshot.IsStableFor(contentPolicy))
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            long catalogVersion = snapshot.Catalog.Values.Aggregate(
                0L,
                static (total, revision) => checked(total + revision.Revision));
            return ApplicationResult<long>.Success(checked(
                snapshot.Passport.Revision
                + (contentPolicy.Includes(ShareContentField.PublicDisplayName)
                    ? snapshot.DisplayName.Revision
                    : 0)
                + (contentPolicy.Includes(ShareContentField.Avatar)
                    ? snapshot.Avatar.Revision
                    : 0)
                + (contentPolicy.Includes(ShareContentField.GlobalRatings)
                    ? snapshot.Ratings.Revision
                    : 0)
                + catalogVersion));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }

    private static ShareSourceRevision ResolveOptionalRevision(
        IReadOnlyDictionary<string, ShareSourceRevision> revisions,
        string scopeKey)
    {
        return revisions.TryGetValue(scopeKey, out ShareSourceRevision? revision)
            ? revision
            : new ShareSourceRevision(0, 0, DateTime.UnixEpoch);
    }

    private static string[] NormalizeSelectedParkIds(IEnumerable<string> selectedParkIds)
    {
        ArgumentNullException.ThrowIfNull(selectedParkIds);
        return selectedParkIds
            .Select(static parkId => parkId?.Trim() ?? string.Empty)
            .Where(static parkId => parkId.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static parkId => parkId, StringComparer.Ordinal)
            .Take(PassportProfileShareInputNormalizer.MaximumSelectedParks)
            .ToArray();
    }
}
