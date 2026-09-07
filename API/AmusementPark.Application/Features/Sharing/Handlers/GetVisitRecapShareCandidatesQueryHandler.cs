using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetVisitRecapShareCandidatesQueryHandler
    : IQueryHandler<
        GetVisitRecapShareCandidatesQuery,
        ApplicationResult<VisitRecapShareCandidatesResult>>
{
    private readonly IVisitRecapSharePreviewBuilder previewBuilder;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IVisitRecapShareSnapshotRepository snapshotRepository;

    public GetVisitRecapShareCandidatesQueryHandler(
        IVisitRecapSharePreviewBuilder previewBuilder,
        ISharePublicationRepository publicationRepository,
        IVisitRecapShareSnapshotRepository snapshotRepository)
    {
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<ApplicationResult<VisitRecapShareCandidatesResult>> HandleAsync(
        GetVisitRecapShareCandidatesQuery query,
        CancellationToken cancellationToken = default)
    {
        string sourceScopeKey;
        try
        {
            sourceScopeKey = VisitRecapShareSourceScope.Create(
                query.OwnerUserId,
                query.VisitId);
        }
        catch (IdentifierValidationException)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.InvalidSource());
        }

        SharePublication? publication = await this.publicationRepository.GetOwnedBySourceAsync(
            query.OwnerUserId,
            SharePublicationType.VisitRecap,
            sourceScopeKey,
            cancellationToken);
        VisitRecapShareSnapshot? snapshot = publication is null
            || publication.PublicationVersion < 1
            ? null
            : await this.snapshotRepository.GetAsync(
                publication.Id,
                publication.PublicationVersion,
                cancellationToken);
        if (publication?.IsResolvable == true && snapshot is null)
        {
            return ApplicationResult<VisitRecapShareCandidatesResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        string[]? savedItemIds = snapshot?.Content.Items
            .Select(static item => item.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        ApplicationResult<VisitRecapShareCandidatesResult> candidates =
            await this.previewBuilder.GetCandidatesAsync(
            query.OwnerUserId,
            query.VisitId,
            query.IncludeMissedItems,
            savedItemIds,
            cancellationToken);
        if (!candidates.IsSuccess || candidates.Value is null)
        {
            return candidates;
        }

        HashSet<string> availableIds = candidates.Value.Items
            .Select(static item => item.ParkItemId)
            .ToHashSet(StringComparer.Ordinal);
        string[]? restorableIds = savedItemIds?
            .Where(availableIds.Contains)
            .ToArray();
        return ApplicationResult<VisitRecapShareCandidatesResult>.Success(
            candidates.Value with
            {
                SavedSelectedParkItemIds = restorableIds,
                SavedPublicCaption = snapshot?.Content.PublicCaption,
                HasSavedSnapshot = snapshot is not null,
            });
    }
}
