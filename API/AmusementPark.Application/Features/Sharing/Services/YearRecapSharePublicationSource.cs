using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class YearRecapSharePublicationSource
    : ISharePublicationSourceDescriptor, IYearRecapShareSourceVersionProvider
{
    private readonly IYearRecapSourceReader sourceReader;
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;

    public YearRecapSharePublicationSource(
        IYearRecapSourceReader sourceReader,
        IShareSourceRevisionRepository sourceRevisionRepository)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
    }

    public SharePublicationType PublicationType => SharePublicationType.YearRecap;

    public ApplicationResult<string> ResolveSourceScopeKey(string ownerUserId, string? sourceId)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0 || !TryParseYear(sourceId, out int year))
        {
            return ApplicationResult<string>.Failure(SharingApplicationErrors.InvalidSource());
        }

        return ApplicationResult<string>.Success(
            YearRecapShareSourceScope.Create(normalizedOwner, year));
    }

    public ShareContentPolicy CreateDefaultPolicy()
    {
        return ShareContentPolicy.Create(
            this.PublicationType,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.RideCount,
                ShareContentField.GeographicStatistics,
            });
    }

    public ApplicationResult<bool> ValidatePolicyForPublication(ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        bool includesUnsupportedContent = contentPolicy.IncludedFields.Any(
            static field => field is not ShareContentField.RideCount
                and not ShareContentField.TemporalRatings
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
        if (!YearRecapShareSourceScope.TryParse(
                request.SourceScopeKey,
                out string ownerUserId,
                out int year))
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        ApplicationResult<YearRecapShareSourceRevision> revision =
            await this.GetOwnedSourceVersionAsync(ownerUserId, year, cancellationToken);
        return revision.IsSuccess && revision.Value is not null
            ? ApplicationResult<long>.Success(revision.Value.Version)
            : ApplicationResult<long>.Failure(revision.Errors);
    }

    public async Task<ApplicationResult<YearRecapShareSourceRevision>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        int year,
        CancellationToken cancellationToken)
    {
        YearRecapSourceData source = await this.sourceReader.ReadOwnedCompletedYearAsync(
            ownerUserId,
            year,
            cancellationToken);
        IReadOnlyDictionary<string, ShareSourceRevision> catalogRevisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[] { PersonalRankingShareSourceScope.PublicCatalog },
                cancellationToken);
        ShareSourceRevision catalogRevision =
            catalogRevisions[PersonalRankingShareSourceScope.PublicCatalog];
        if (!source.IsStable || !catalogRevision.IsStable)
        {
            return ApplicationResult<YearRecapShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        ShareSourceRevision recapRevision =
            await this.sourceRevisionRepository.ReconcileFingerprintAsync(
                YearRecapShareSourceScope.Create(ownerUserId, year),
                source.SourceFingerprint,
                cancellationToken);
        if (!recapRevision.IsStable)
        {
            return ApplicationResult<YearRecapShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            long version = checked(recapRevision.Revision + catalogRevision.Revision);
            return ApplicationResult<YearRecapShareSourceRevision>.Success(
                new YearRecapShareSourceRevision(version, source.SourceFingerprint));
        }
        catch (OverflowException)
        {
            return ApplicationResult<YearRecapShareSourceRevision>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }
    }

    public static bool TryParseYear(string? value, out int year)
    {
        return int.TryParse(
                value?.Trim(),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out year)
            && year >= DateOnly.MinValue.Year
            && year <= DateOnly.MaxValue.Year;
    }
}
