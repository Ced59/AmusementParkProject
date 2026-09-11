using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class YearRecapShareSnapshotWriter : ISharePublicationSnapshotWriter
{
    private readonly IYearRecapSharePreviewBuilder previewBuilder;
    private readonly IYearRecapShareSnapshotRepository snapshotRepository;
    private readonly TimeProvider timeProvider;

    public YearRecapShareSnapshotWriter(
        IYearRecapSharePreviewBuilder previewBuilder,
        IYearRecapShareSnapshotRepository snapshotRepository,
        TimeProvider? timeProvider = null)
    {
        this.previewBuilder = previewBuilder
            ?? throw new ArgumentNullException(nameof(previewBuilder));
        this.snapshotRepository = snapshotRepository
            ?? throw new ArgumentNullException(nameof(snapshotRepository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SharePublicationType PublicationType => SharePublicationType.YearRecap;

    public async Task<ApplicationResult<bool>> WriteAsync(
        SharePublicationSnapshotWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!YearRecapSharePublicationSource.TryParseYear(request.SourceId, out int year))
        {
            return ApplicationResult<bool>.Failure(SharingApplicationErrors.InvalidSource());
        }

        ApplicationResult<SharePublicationPreviewResult> previewResult =
            await this.previewBuilder.BuildAsync(
                request.OwnerUserId,
                year,
                request.ContentPolicy,
                request.YearRecap,
                cancellationToken);
        if (!previewResult.IsSuccess || previewResult.Value?.YearRecap is null)
        {
            return ApplicationResult<bool>.Failure(previewResult.Errors);
        }

        SharePublicationPreviewResult preview = previewResult.Value;
        if (preview.YearRecap.IsEmpty)
        {
            return ApplicationResult<bool>.Failure(SharingApplicationErrors.EmptyYearRecap());
        }

        if (preview.SourceVersion != request.SourceVersion
            || !string.Equals(
                preview.ContentFingerprint,
                request.ContentFingerprint,
                StringComparison.Ordinal))
        {
            return ApplicationResult<bool>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired());
        }

        YearRecapShareSnapshot snapshot = new YearRecapShareSnapshot(
            request.PublicationId,
            request.PublicationVersion,
            request.PublicationStateVersion,
            request.SourceVersion,
            request.ContentPolicy.SchemaVersion,
            request.ContentPolicy.DatePrecision,
            request.ContentPolicy.IncludedFields,
            request.ContentFingerprint,
            preview.YearRecap,
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
