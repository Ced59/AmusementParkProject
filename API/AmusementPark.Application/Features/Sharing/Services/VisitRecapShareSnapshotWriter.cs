using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class VisitRecapShareSnapshotWriter : ISharePublicationSnapshotWriter
{
    private readonly IVisitRecapSharePreviewBuilder previewBuilder;
    private readonly IVisitRecapShareSnapshotRepository snapshotRepository;
    private readonly TimeProvider timeProvider;

    public VisitRecapShareSnapshotWriter(
        IVisitRecapSharePreviewBuilder previewBuilder,
        IVisitRecapShareSnapshotRepository snapshotRepository,
        TimeProvider? timeProvider = null)
    {
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SharePublicationType PublicationType => SharePublicationType.VisitRecap;

    public async Task<ApplicationResult<bool>> WriteAsync(
        SharePublicationSnapshotWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.SourceId))
        {
            return ApplicationResult<bool>.Failure(SharingApplicationErrors.InvalidSource());
        }

        ApplicationResult<SharePublicationPreviewResult> previewResult =
            await this.previewBuilder.BuildAsync(
                request.OwnerUserId,
                request.SourceId,
                request.ContentPolicy,
                request.VisitRecap,
                cancellationToken);
        if (!previewResult.IsSuccess || previewResult.Value?.VisitRecap is null)
        {
            return ApplicationResult<bool>.Failure(previewResult.Errors);
        }

        SharePublicationPreviewResult preview = previewResult.Value;
        if (preview.SourceVersion != request.SourceVersion
            || !string.Equals(
                preview.ContentFingerprint,
                request.ContentFingerprint,
                StringComparison.Ordinal))
        {
            return ApplicationResult<bool>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired());
        }

        VisitRecapShareSnapshot snapshot = new VisitRecapShareSnapshot(
            request.PublicationId,
            request.PublicationVersion,
            request.PublicationStateVersion,
            request.SourceVersion,
            request.ContentPolicy.SchemaVersion,
            request.ContentPolicy.DatePrecision,
            request.ContentPolicy.IncludedFields,
            request.ContentFingerprint,
            preview.VisitRecap,
            this.timeProvider.GetUtcNow().UtcDateTime);
        bool persisted = await this.snapshotRepository.UpsertAsync(snapshot, cancellationToken);
        return persisted
            ? ApplicationResult<bool>.Success(true)
            : ApplicationResult<bool>.Failure(
                SharingApplicationErrors.PublicationChangedConcurrently());
    }

    public async Task<ApplicationResult<bool>> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken)
    {
        bool deleted = await this.snapshotRepository.DeleteSupersededAsync(
            publicationId,
            publishedVersion,
            cancellationToken);
        return deleted
            ? ApplicationResult<bool>.Success(true)
            : ApplicationResult<bool>.Failure(
                SharingApplicationErrors.PublicationChangedConcurrently());
    }
}
