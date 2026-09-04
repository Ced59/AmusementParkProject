using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class DeleteVisitCommandHandler
    : ICommandHandler<DeleteVisitCommand, ApplicationResult<VisitDeletionReceipt>>
{
    private const int MaximumClientOperationIdLength = 128;
    private readonly IUserVisitRepository visitRepository;
    private readonly IVisitDeletionStore deletionStore;
    private readonly IPassportExportRepository exportRepository;
    private readonly IVisitContentMutationLeaseManager contentMutationLeaseManager;
    private readonly VisitPurgeScheduler purgeScheduler;
    private readonly IPassportAuditPublisher auditPublisher;
    private readonly IPassportClock clock;

    public DeleteVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IVisitDeletionStore deletionStore,
        IPassportExportRepository exportRepository,
        IVisitContentMutationLeaseManager contentMutationLeaseManager,
        VisitPurgeScheduler purgeScheduler,
        IPassportAuditPublisher auditPublisher,
        IPassportClock clock)
    {
        this.visitRepository = visitRepository;
        this.deletionStore = deletionStore;
        this.exportRepository = exportRepository;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
        this.purgeScheduler = purgeScheduler;
        this.auditPublisher = auditPublisher;
        this.clock = clock;
    }

    public async Task<ApplicationResult<VisitDeletionReceipt>> HandleAsync(
        DeleteVisitCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId)
            || !VisitId.TryParse(command.VisitId, out VisitId visitId))
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        string? clientOperationId = NormalizeClientOperationId(command.ClientOperationId);
        if (clientOperationId is null)
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.InvalidIdempotencyKey());
        }

        string userId = command.UserId.Trim();
        VisitDeletionReceipt? replay = await this.deletionStore.GetReceiptAsync(
            visitId,
            userId,
            clientOperationId,
            cancellationToken);
        if (replay is not null)
        {
            await this.purgeScheduler.ScheduleAsync(
                visitId,
                userId,
                replay.DeletionVersion,
                GetRemainingPurgeDelay(replay.PurgeScheduledForUtc, this.clock.UtcNow),
                cancellationToken);
            await this.exportRepository.InvalidateOwnedAsync(
                userId,
                replay.DeletedAtUtc,
                cancellationToken);
            return ApplicationResult<VisitDeletionReceipt>.Success(
                replay with { WasReplayed = true });
        }

        if (command.ExpectedVersion < 1
            || command.ConfirmedOccurrenceCount < 0
            || command.ConfirmedAssessmentCount < 0)
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.InvalidDeletionConfirmation());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        if (visit.Version != command.ExpectedVersion)
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.VisitConcurrencyConflict());
        }

        DateTime leaseAcquiredAtUtc = this.clock.UtcNow;
        IVisitContentMutationLease? contentMutationLease = visit.Status == VisitStatus.Draft
            ? await this.contentMutationLeaseManager.TryAcquireAsync(
                visit,
                leaseAcquiredAtUtc,
                cancellationToken)
            : null;
        if (visit.Status == VisitStatus.Draft && contentMutationLease is null)
        {
            return ApplicationResult<VisitDeletionReceipt>.Failure(
                PassportApplicationErrors.VisitConcurrencyConflict());
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;
        using CancellationTokenSource? leaseCancellationSource =
            PassportLeaseCancellation.Link(contentMutationLease, cancellationToken);
        CancellationToken guardedCancellationToken =
            leaseCancellationSource?.Token ?? cancellationToken;
        VisitDeletionImpact impact = await this.deletionStore.GetImpactAsync(
            visit.Id,
            visit.UserId,
            guardedCancellationToken);
        long assessmentCount = impact.AssessmentCount
            + (visit.ParkAssessment is null ? 0 : 1);
        if (impact.OccurrenceCount != command.ConfirmedOccurrenceCount
            || assessmentCount != command.ConfirmedAssessmentCount)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitDeletionReceipt>.Failure(
                    PassportApplicationErrors.DeletionPreviewChanged()));
        }

        DateTime deletedAtUtc = this.clock.UtcNow;
        DateTime purgeScheduledForUtc = deletedAtUtc.Add(VisitDeletionPolicy.Retention);
        PassportAuditEvent auditEvent = VisitDeletionAuditEventFactory.Create(
            visit,
            deletedAtUtc);
        bool deleted = await this.deletionStore.TryTombstoneAsync(
            new VisitDeletionTombstoneRequest(
                visit.Id,
                visit.UserId,
                visit.Version,
                clientOperationId,
                deletedAtUtc,
                purgeScheduledForUtc,
                contentMutationLease?.Token,
                auditEvent),
            guardedCancellationToken);
        if (!deleted)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitDeletionReceipt>.Failure(
                    PassportApplicationErrors.VisitConcurrencyConflict()));
        }
        contentMutationLease?.MarkMutationCompleted();

        await this.purgeScheduler.ScheduleAsync(
            visit.Id,
            visit.UserId,
            visit.Version + 1,
            GetRemainingPurgeDelay(purgeScheduledForUtc, this.clock.UtcNow),
            cancellationToken);
        await this.exportRepository.InvalidateOwnedAsync(
            visit.UserId,
            deletedAtUtc,
            cancellationToken);

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return ApplicationResult<VisitDeletionReceipt>.Success(
            new VisitDeletionReceipt(
                visit.Id.Value,
                deletedAtUtc,
                purgeScheduledForUtc,
                visit.Version + 1,
                false));
    }

    private static TimeSpan GetRemainingPurgeDelay(
        DateTime purgeScheduledForUtc,
        DateTime nowUtc)
    {
        TimeSpan remaining = purgeScheduledForUtc - nowUtc;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static string? NormalizeClientOperationId(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > MaximumClientOperationIdLength
            || normalized.Any(char.IsControl))
        {
            return null;
        }

        return normalized;
    }
}
