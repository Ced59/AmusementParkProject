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

        ApplicationResult<PassportProfileShareInput> selectionResult =
            await this.ResolveSelectionAsync(request, cancellationToken);
        if (!selectionResult.IsSuccess || selectionResult.Value is null)
        {
            return ApplicationResult<long>.Failure(selectionResult.Errors);
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            ownerUserId,
            request.ContentPolicy,
            selectionResult.Value,
            cancellationToken);
        return CreateVersion(snapshot, request.ContentPolicy);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        PrepareOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            PassportProfileShareInput input,
            CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0)
        {
            return ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        if (RequiresPassport(contentPolicy))
        {
            string[] passportScopes = ResolvePassportScopes(normalizedOwnerUserId, input);
            string fingerprintScope = ResolveFingerprintScope(normalizedOwnerUserId, input);
            await this.sourceRevisionRepository.EnsureCreatedAsync(
                passportScopes.Append(fingerprintScope).ToArray(),
                cancellationToken);
        }

        return await this.GetOwnedSourceRevisionSnapshotAsync(
            normalizedOwnerUserId,
            contentPolicy,
            input,
            cancellationToken);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevisionSnapshot>>
        GetOwnedSourceRevisionSnapshotAsync(
            string ownerUserId,
            ShareContentPolicy contentPolicy,
            PassportProfileShareInput input,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        ApplicationResult<PassportProfileShareInput> normalizedResult =
            PassportProfileShareInputNormalizer.Normalize(input, contentPolicy);
        if (normalizedOwnerUserId.Length == 0
            || !normalizedResult.IsSuccess
            || normalizedResult.Value is null
            || !HasSelection(normalizedResult.Value))
        {
            return ApplicationResult<PassportProfileShareSourceRevisionSnapshot>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSourceRevisionSnapshot snapshot = await this.ReadSnapshotAsync(
            normalizedOwnerUserId,
            contentPolicy,
            normalizedResult.Value,
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
        PassportProfileShareInput input,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedSnapshot);
        ArgumentNullException.ThrowIfNull(contentPolicy);
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        ApplicationResult<PassportProfileShareInput> normalizedResult =
            PassportProfileShareInputNormalizer.Normalize(input, contentPolicy);
        if (normalizedOwnerUserId.Length == 0
            || !normalizedResult.IsSuccess
            || normalizedResult.Value is null
            || !HasSelection(normalizedResult.Value)
            || !expectedSnapshot.IsStableFor(contentPolicy))
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        PassportProfileShareSourceRevisionSnapshot snapshotAfter = await this.ReadSnapshotAsync(
            normalizedOwnerUserId,
            contentPolicy,
            normalizedResult.Value,
            cancellationToken);
        if (!snapshotAfter.IsStableFor(contentPolicy)
            || !expectedSnapshot.HasSameRevisionsAs(snapshotAfter, contentPolicy))
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        PassportProfileShareSourceRevisionSnapshot reconciledSnapshot = snapshotAfter;
        if (RequiresPassport(contentPolicy))
        {
            ShareSourceRevision fingerprintRevision =
                await this.sourceRevisionRepository.ReconcileFingerprintAsync(
                    ResolveFingerprintScope(normalizedOwnerUserId, normalizedResult.Value),
                    sourceFingerprint,
                    cancellationToken);
            if (!fingerprintRevision.IsStable)
            {
                return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                    SharingApplicationErrors.SourceChangedDuringPreview());
            }

            reconciledSnapshot = snapshotAfter with
            {
                Fingerprint = fingerprintRevision,
            };
        }

        ApplicationResult<long> versionResult = CreateVersion(
            reconciledSnapshot,
            contentPolicy);
        return versionResult.IsSuccess
            ? ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(
                    versionResult.Value,
                    sourceFingerprint))
            : ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                versionResult.Errors);
    }

    private async Task<ApplicationResult<PassportProfileShareInput>> ResolveSelectionAsync(
        SharePublicationSourceVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PassportProfile is not null)
        {
            ApplicationResult<PassportProfileShareInput> normalized =
                PassportProfileShareInputNormalizer.Normalize(
                    request.PassportProfile,
                    request.ContentPolicy);
            return normalized.IsSuccess
                && normalized.Value is not null
                && HasSelection(normalized.Value)
                    ? normalized
                    : ApplicationResult<PassportProfileShareInput>.Failure(
                        SharingApplicationErrors.SourceUnavailable());
        }

        if (request.PublicationId is null
            || !request.PublicationVersion.HasValue
            || request.PublicationVersion.Value <= 0)
        {
            return ApplicationResult<PassportProfileShareInput>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        PassportProfileShareSnapshot? snapshot = await this.snapshotRepository.GetAsync(
            request.PublicationId.Value,
            request.PublicationVersion.Value,
            cancellationToken);
        ApplicationResult<PassportProfileShareInput> persisted =
            PassportProfileShareInputNormalizer.Normalize(
                snapshot?.Selection,
                request.ContentPolicy);
        return persisted.IsSuccess
            && persisted.Value is not null
            && HasSelection(persisted.Value)
                ? persisted
                : ApplicationResult<PassportProfileShareInput>.Failure(
                    SharingApplicationErrors.SourceUnavailable());
    }

    private async Task<PassportProfileShareSourceRevisionSnapshot> ReadSnapshotAsync(
        string ownerUserId,
        ShareContentPolicy contentPolicy,
        PassportProfileShareInput input,
        CancellationToken cancellationToken)
    {
        bool requiresPassport = RequiresPassport(contentPolicy);
        string[] passportScopes = requiresPassport
            ? ResolvePassportScopes(ownerUserId, input)
            : Array.Empty<string>();
        string? fingerprintScope = requiresPassport
            ? ResolveFingerprintScope(ownerUserId, input)
            : null;
        string displayNameScope = PublicIdentityShareSourceScope.CreateDisplayName(ownerUserId);
        string avatarScope = PublicIdentityShareSourceScope.CreateAvatar(ownerUserId);
        string[] ratingsScopes = contentPolicy.Includes(ShareContentField.GlobalRatings)
            ? NormalizeSelectedRatingKeys(input.SelectedRatingKeys)
                .Select(selectionKey => PersonalRankingShareSourceScope.CreateRating(
                    ownerUserId,
                    selectionKey))
                .ToArray()
            : Array.Empty<string>();
        string[] catalogScopes = RequiresCatalog(contentPolicy)
            ? NormalizeSelectedParkIds(input.SelectedParkIds ?? Array.Empty<string>())
                .Select(PublicCatalogShareSourceScope.CreatePark)
                .ToArray()
            : Array.Empty<string>();
        List<string> scopes = new List<string>(passportScopes);
        if (fingerprintScope is not null)
        {
            scopes.Add(fingerprintScope);
        }

        if (contentPolicy.Includes(ShareContentField.PublicDisplayName))
        {
            scopes.Add(displayNameScope);
        }

        if (contentPolicy.Includes(ShareContentField.Avatar))
        {
            scopes.Add(avatarScope);
        }

        scopes.AddRange(ratingsScopes);
        scopes.AddRange(catalogScopes);
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(scopes, cancellationToken);
        Dictionary<string, ShareSourceRevision> catalog = catalogScopes.ToDictionary(
            static scope => scope,
            scope => revisions[scope],
            StringComparer.Ordinal);
        return new PassportProfileShareSourceRevisionSnapshot(
            AggregateRevisions(passportScopes.Select(scope => revisions[scope])),
            fingerprintScope is null
                ? new ShareSourceRevision(0, 0, DateTime.UnixEpoch)
                : revisions[fingerprintScope],
            ResolveOptionalRevision(revisions, displayNameScope),
            ResolveOptionalRevision(revisions, avatarScope),
            AggregateRevisions(ratingsScopes.Select(scope => revisions[scope])),
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
                + snapshot.Fingerprint.Revision
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

    private static string[] NormalizeSelectedRatingKeys(IEnumerable<string>? selectedRatingKeys)
    {
        return (selectedRatingKeys ?? Array.Empty<string>())
            .Select(static selectionKey => selectionKey?.Trim() ?? string.Empty)
            .Where(static selectionKey => selectionKey.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static selectionKey => selectionKey, StringComparer.Ordinal)
            .Take(PassportProfileShareInputNormalizer.MaximumSelectedRatings)
            .ToArray();
    }

    private static string[] ResolvePassportScopes(
        string ownerUserId,
        PassportProfileShareInput input)
    {
        int[] years = (input.SelectedYears ?? Array.Empty<int>())
            .Distinct()
            .OrderBy(static year => year)
            .ToArray();
        string[] parkIds = NormalizeSelectedParkIds(
            input.SelectedParkIds ?? Array.Empty<string>());
        return years
            .SelectMany(year => parkIds.Select(parkId =>
                PassportProfileShareSourceScope.CreateSegment(ownerUserId, year, parkId)))
            .OrderBy(static scope => scope, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ResolveFingerprintScope(
        string ownerUserId,
        PassportProfileShareInput input)
    {
        return PassportProfileShareSourceScope.CreateFingerprint(
            ownerUserId,
            input.SelectedYears ?? Array.Empty<int>(),
            input.SelectedParkIds ?? Array.Empty<string>());
    }

    private static ShareSourceRevision AggregateRevisions(
        IEnumerable<ShareSourceRevision> source)
    {
        ShareSourceRevision[] revisions = source.ToArray();
        return revisions.Length == 0
            ? new ShareSourceRevision(0, 0, DateTime.UnixEpoch)
            : new ShareSourceRevision(
                revisions.Aggregate(
                    0L,
                    static (total, revision) => checked(total + revision.Revision)),
                revisions.Sum(static revision => revision.PendingMutationCount),
                revisions.Max(static revision => revision.UpdatedAtUtc));
    }

    private static bool HasSelection(PassportProfileShareInput input)
    {
        return input.SelectedYears?.Count > 0 && input.SelectedParkIds?.Count > 0;
    }

    private static bool RequiresCatalog(ShareContentPolicy contentPolicy)
    {
        return RequiresPassport(contentPolicy);
    }

    private static bool RequiresPassport(ShareContentPolicy contentPolicy)
    {
        return contentPolicy.Includes(ShareContentField.RideCount)
            || contentPolicy.Includes(ShareContentField.TemporalRatings)
            || contentPolicy.Includes(ShareContentField.GlobalRatings)
            || contentPolicy.Includes(ShareContentField.GeographicStatistics)
            || contentPolicy.Includes(ShareContentField.MissedItems);
    }
}
