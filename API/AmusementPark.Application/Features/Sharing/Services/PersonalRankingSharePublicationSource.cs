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
                ShareContentField.Avatar,
                ShareContentField.GlobalRatings,
            });
    }

    public async Task<ApplicationResult<long>> GetCurrentSourceVersionAsync(
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        ShareSourceRevision ownerRevisionBefore = await this.sourceRevisionRepository.GetOrCreateAsync(
            sourceScopeKey,
            cancellationToken);
        ShareSourceRevision catalogRevisionBefore = await this.sourceRevisionRepository.GetOrCreateAsync(
            PersonalRankingShareSourceScope.PublicCatalog,
            cancellationToken);
        ShareSourceRevision ownerRevisionAfter = await this.sourceRevisionRepository.GetOrCreateAsync(
            sourceScopeKey,
            cancellationToken);
        ShareSourceRevision catalogRevisionAfter = await this.sourceRevisionRepository.GetOrCreateAsync(
            PersonalRankingShareSourceScope.PublicCatalog,
            cancellationToken);
        if (!ownerRevisionBefore.IsStable
            || !catalogRevisionBefore.IsStable
            || !ownerRevisionAfter.IsStable
            || !catalogRevisionAfter.IsStable
            || ownerRevisionBefore.Revision != ownerRevisionAfter.Revision
            || catalogRevisionBefore.Revision != catalogRevisionAfter.Revision)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        try
        {
            return ApplicationResult<long>.Success(
                checked(ownerRevisionAfter.Revision + catalogRevisionAfter.Revision));
        }
        catch (OverflowException)
        {
            return ApplicationResult<long>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }
    }
}
