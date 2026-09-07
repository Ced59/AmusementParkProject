using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PersonalRankingSharePublicationSource : ISharePublicationSourceDescriptor
{
    private readonly IShareSourceRevisionRepository sourceRevisionRepository;

    public PersonalRankingSharePublicationSource(
        IShareSourceRevisionRepository sourceRevisionRepository)
    {
        this.sourceRevisionRepository = sourceRevisionRepository
            ?? throw new ArgumentNullException(nameof(sourceRevisionRepository));
    }

    public SharePublicationType PublicationType => SharePublicationType.PersonalRanking;

    public ApplicationResult<string> ResolveSourceScopeKey(string ownerUserId, string? sourceId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId) || !string.IsNullOrWhiteSpace(sourceId))
        {
            return ApplicationResult<string>.Failure(SharingApplicationErrors.InvalidSource());
        }

        return ApplicationResult<string>.Success(
            PersonalRankingShareSourceScope.Create(ownerUserId));
    }

    public ShareContentPolicy CreateDefaultPolicy()
    {
        return ShareContentPolicy.Create(
            this.PublicationType,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.GlobalRatings,
            });
    }

    public ApplicationResult<bool> ValidatePolicyForPublication(ShareContentPolicy contentPolicy)
    {
        ArgumentNullException.ThrowIfNull(contentPolicy);
        if (contentPolicy.PublicationType != this.PublicationType
            || !contentPolicy.Includes(ShareContentField.GlobalRatings))
        {
            return ApplicationResult<bool>.Failure(
                SharingApplicationErrors.RequiredPublicContentMissing());
        }

        bool includesUnsupportedContent = contentPolicy.IncludedFields.Any(
            static field => field is not ShareContentField.PublicDisplayName
                and not ShareContentField.GlobalRatings);
        return includesUnsupportedContent
            ? ApplicationResult<bool>.Failure(
                SharingApplicationErrors.PublicContentNotSupported())
            : ApplicationResult<bool>.Success(true);
    }

    public async Task<ApplicationResult<long>> GetCurrentSourceVersionAsync(
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, ShareSourceRevision> revisions =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                new[]
                {
                    sourceScopeKey,
                    PersonalRankingShareSourceScope.PublicCatalog,
                },
                cancellationToken);
        ShareSourceRevision ownerRevision = revisions[sourceScopeKey];
        ShareSourceRevision catalogRevision = revisions[PersonalRankingShareSourceScope.PublicCatalog];
        if (!ownerRevision.IsStable || !catalogRevision.IsStable)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            return ApplicationResult<long>.Success(
                checked(ownerRevision.Revision + catalogRevision.Revision));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }
}
