using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetSharedVisitRecapQueryHandler
    : IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>>
{
    private readonly ISharePublicationAccessResolver accessResolver;
    private readonly IVisitRecapShareSnapshotRepository snapshotRepository;

    public GetSharedVisitRecapQueryHandler(
        ISharePublicationAccessResolver accessResolver,
        IVisitRecapShareSnapshotRepository snapshotRepository)
    {
        this.accessResolver = accessResolver
            ?? throw new ArgumentNullException(nameof(accessResolver));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<ApplicationResult<SharedVisitRecapResult>> HandleAsync(
        GetSharedVisitRecapQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedSharePublicationResult> resolution =
            await this.accessResolver.ResolveAsync(
                query.ShareId,
                SharePublicationType.VisitRecap,
                cancellationToken);
        if (!resolution.IsSuccess || resolution.Value is null)
        {
            return ApplicationResult<SharedVisitRecapResult>.Failure(resolution.Errors);
        }

        ResolvedSharePublicationResult publication = resolution.Value;
        if (!SharePublicationId.TryParse(
                publication.PublicationId,
                out SharePublicationId publicationId))
        {
            return NotFound();
        }

        VisitRecapShareSnapshot? snapshot = await this.snapshotRepository.GetAsync(
            publicationId,
            publication.PublicationVersion,
            cancellationToken);
        if (snapshot is null
            || snapshot.SourceVersion != publication.SourceVersion
            || snapshot.PolicySchemaVersion != publication.ContentPolicy.SchemaVersion
            || snapshot.DatePrecision != publication.ContentPolicy.DatePrecision
            || !snapshot.IncludedFields.SequenceEqual(publication.ContentPolicy.IncludedFields)
            || !string.Equals(
                snapshot.ContentFingerprint,
                publication.ContentFingerprint,
                StringComparison.Ordinal))
        {
            return NotFound();
        }

        ApplicationResult<bool> revalidation = await this.accessResolver.RevalidateAsync(
            query.ShareId,
            publication,
            cancellationToken);
        return revalidation.IsSuccess
            ? ApplicationResult<SharedVisitRecapResult>.Success(
                new SharedVisitRecapResult(
                    publication.PublishedAtUtc,
                    snapshot.Content))
            : ApplicationResult<SharedVisitRecapResult>.Failure(revalidation.Errors);
    }

    private static ApplicationResult<SharedVisitRecapResult> NotFound()
    {
        return ApplicationResult<SharedVisitRecapResult>.Failure(
            SharingApplicationErrors.SnapshotUnavailable());
    }
}
