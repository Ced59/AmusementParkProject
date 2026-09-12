using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetSharedPassportProfileQueryHandler
    : IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>>
{
    private readonly ISharePublicationAccessResolver accessResolver;
    private readonly IPassportProfileShareSnapshotRepository snapshotRepository;

    public GetSharedPassportProfileQueryHandler(
        ISharePublicationAccessResolver accessResolver,
        IPassportProfileShareSnapshotRepository snapshotRepository)
    {
        this.accessResolver = accessResolver ?? throw new ArgumentNullException(nameof(accessResolver));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task<ApplicationResult<SharedPassportProfileResult>> HandleAsync(
        GetSharedPassportProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedSharePublicationResult> resolution =
            await this.accessResolver.ResolveAsync(
                query.ShareId,
                SharePublicationType.PassportProfile,
                cancellationToken);
        if (!resolution.IsSuccess || resolution.Value is null)
        {
            return ApplicationResult<SharedPassportProfileResult>.Failure(resolution.Errors);
        }

        ResolvedSharePublicationResult publication = resolution.Value;
        if (!SharePublicationId.TryParse(
                publication.PublicationId,
                out SharePublicationId publicationId))
        {
            return NotFound();
        }

        PassportProfileShareSnapshot? snapshot = await this.snapshotRepository.GetAsync(
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
            ? ApplicationResult<SharedPassportProfileResult>.Success(
                new SharedPassportProfileResult(
                    publication.PublishedAtUtc,
                    snapshot.Content))
            : ApplicationResult<SharedPassportProfileResult>.Failure(revalidation.Errors);
    }

    private static ApplicationResult<SharedPassportProfileResult> NotFound()
    {
        return ApplicationResult<SharedPassportProfileResult>.Failure(
            SharingApplicationErrors.SnapshotUnavailable());
    }
}
