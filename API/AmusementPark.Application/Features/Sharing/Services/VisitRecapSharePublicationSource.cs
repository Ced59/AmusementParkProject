using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class VisitRecapSharePublicationSource
    : ISharePublicationSourceDescriptor, IVisitRecapShareSourceVersionProvider
{
    private readonly IVisitRecapSourceReader sourceReader;
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;

    public VisitRecapSharePublicationSource(
        IVisitRecapSourceReader sourceReader,
        IShareSourceRevisionRepository sourceRevisionRepository)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
    }

    public SharePublicationType PublicationType => SharePublicationType.VisitRecap;

    public ApplicationResult<string> ResolveSourceScopeKey(string ownerUserId, string? sourceId)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        string normalizedSource = sourceId?.Trim() ?? string.Empty;
        try
        {
            _ = VisitId.Parse(normalizedSource);
        }
        catch (ArgumentException)
        {
            return InvalidSource();
        }

        return normalizedOwner.Length == 0
            ? InvalidSource()
            : ApplicationResult<string>.Success(
                VisitRecapShareSourceScope.Create(normalizedOwner, normalizedSource));
    }

    public ShareContentPolicy CreateDefaultPolicy()
    {
        return ShareContentPolicy.Create(
            this.PublicationType,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.RideCount });
    }

    public ApplicationResult<bool> ValidatePolicyForPublication(ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        if (contentPolicy.PublicationType != this.PublicationType)
        {
            return ApplicationResult<bool>.Failure(
                SharingApplicationErrors.InvalidPublicationType());
        }

        bool includesUnsupportedContent = contentPolicy.IncludedFields.Any(
            static field => field is not ShareContentField.RideCount
                and not ShareContentField.TemporalRatings
                and not ShareContentField.PublicCaption
                and not ShareContentField.MissedItems);
        return includesUnsupportedContent
            ? ApplicationResult<bool>.Failure(
                SharingApplicationErrors.PublicContentNotSupported())
            : ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<long>> GetCurrentSourceVersionAsync(
        string sourceScopeKey,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        if (!VisitRecapShareSourceScope.TryParse(
                sourceScopeKey,
                out string ownerUserId,
                out string visitId))
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        return await this.GetOwnedSourceVersionAsync(ownerUserId, visitId, cancellationToken);
    }

    public async Task<ApplicationResult<long>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken)
    {
        VisitRecapSourceRevision? visitRevision =
            await this.sourceReader.GetOwnedCompletedRevisionAsync(
                ownerUserId,
                visitId,
                cancellationToken);
        if (visitRevision is null)
        {
            return ApplicationResult<long>.Failure(SharingApplicationErrors.SourceUnavailable());
        }

        IReadOnlyDictionary<string, ShareSourceRevision> catalogRevisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[] { PersonalRankingShareSourceScope.PublicCatalog },
                cancellationToken);
        ShareSourceRevision catalogRevision =
            catalogRevisions[PersonalRankingShareSourceScope.PublicCatalog];
        if (!visitRevision.IsStable || !catalogRevision.IsStable)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            return ApplicationResult<long>.Success(
                checked(visitRevision.Version + catalogRevision.Revision));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }

    private static ApplicationResult<string> InvalidSource()
    {
        return ApplicationResult<string>.Failure(SharingApplicationErrors.InvalidSource());
    }
}
