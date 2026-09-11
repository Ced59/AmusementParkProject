using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetYearRecapShareSelectionQueryHandler
    : IQueryHandler<GetYearRecapShareSelectionQuery, ApplicationResult<YearRecapShareSelectionResult>>
{
    private readonly ISharePublicationRepository publicationRepository;
    private readonly IYearRecapShareSnapshotRepository snapshotRepository;

    public GetYearRecapShareSelectionQueryHandler(
        ISharePublicationRepository publicationRepository,
        IYearRecapShareSnapshotRepository snapshotRepository)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<ApplicationResult<YearRecapShareSelectionResult>> HandleAsync(
        GetYearRecapShareSelectionQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.OwnerUserId)
            || query.Year < DateOnly.MinValue.Year
            || query.Year > DateOnly.MaxValue.Year)
        {
            return ApplicationResult<YearRecapShareSelectionResult>.Failure(
                SharingApplicationErrors.InvalidSource());
        }

        string sourceScopeKey = YearRecapShareSourceScope.Create(
            query.OwnerUserId,
            query.Year);
        SharePublication? publication = await this.publicationRepository.GetOwnedBySourceAsync(
            query.OwnerUserId,
            SharePublicationType.YearRecap,
            sourceScopeKey,
            cancellationToken);
        YearRecapShareSnapshot? snapshot = publication is null
            || publication.PublicationVersion < 1
            ? null
            : await this.snapshotRepository.GetAsync(
                publication.Id,
                publication.PublicationVersion,
                cancellationToken);
        if (publication?.IsResolvable == true && snapshot is null)
        {
            return ApplicationResult<YearRecapShareSelectionResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        return ApplicationResult<YearRecapShareSelectionResult>.Success(
            new YearRecapShareSelectionResult(
                snapshot?.Content.PublicCaption,
                snapshot is not null));
    }
}
