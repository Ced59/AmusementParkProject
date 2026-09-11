using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileSharePublicationSource
    : ISharePublicationSourceDescriptor, IPassportProfileShareSourceVersionProvider
{
    private readonly IPassportProfileSourceReader sourceReader;
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;

    public PassportProfileSharePublicationSource(
        IPassportProfileSourceReader sourceReader,
        IShareSourceRevisionRepository sourceRevisionRepository)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
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
        CancellationToken cancellationToken)
    {
        if (!PassportProfileShareSourceScope.TryParse(sourceScopeKey, out string ownerUserId))
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[]
                {
                    sourceScopeKey,
                    identityScope,
                    PersonalRankingShareSourceScope.PublicCatalog,
                },
                cancellationToken);
        ShareSourceRevision passportRevision = revisions[sourceScopeKey];
        ShareSourceRevision identityRevision = revisions[identityScope];
        ShareSourceRevision catalogRevision = revisions[PersonalRankingShareSourceScope.PublicCatalog];
        return CreateVersion(passportRevision, identityRevision, catalogRevision);
    }

    public async Task<ApplicationResult<PassportProfileShareSourceRevision>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        PassportProfileSourceData source = await this.sourceReader.ReadOwnedCompletedPassportAsync(
            ownerUserId,
            cancellationToken);
        string identityScope = PersonalRankingShareSourceScope.Create(ownerUserId);
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[]
                {
                    identityScope,
                    PersonalRankingShareSourceScope.PublicCatalog,
                },
                cancellationToken);
        ShareSourceRevision identityRevision = revisions[identityScope];
        ShareSourceRevision catalogRevision = revisions[PersonalRankingShareSourceScope.PublicCatalog];
        if (!source.IsStable || !identityRevision.IsStable || !catalogRevision.IsStable)
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ShareSourceRevision passportRevision =
            await this.sourceRevisionRepository.ReconcileFingerprintAsync(
                PassportProfileShareSourceScope.Create(ownerUserId),
                source.SourceFingerprint,
                cancellationToken);
        if (!passportRevision.IsStable)
        {
            return ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ApplicationResult<long> versionResult = CreateVersion(
            passportRevision,
            identityRevision,
            catalogRevision);
        return versionResult.IsSuccess
            ? ApplicationResult<PassportProfileShareSourceRevision>.Success(
                new PassportProfileShareSourceRevision(
                    versionResult.Value,
                    source.SourceFingerprint))
            : ApplicationResult<PassportProfileShareSourceRevision>.Failure(
                versionResult.Errors);
    }

    private static ApplicationResult<long> CreateVersion(
        ShareSourceRevision passportRevision,
        ShareSourceRevision identityRevision,
        ShareSourceRevision catalogRevision)
    {
        if (!passportRevision.IsStable
            || !identityRevision.IsStable
            || !catalogRevision.IsStable)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            return ApplicationResult<long>.Success(checked(
                passportRevision.Revision
                + identityRevision.Revision
                + catalogRevision.Revision));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }
}
