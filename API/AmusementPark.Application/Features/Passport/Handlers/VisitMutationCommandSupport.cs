using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

internal static class VisitMutationCommandSupport
{
    public static async Task<ApplicationResult<Visit>> LoadAsync(
        string userId,
        string visitIdValue,
        long expectedVersion,
        IUserVisitRepository visitRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApplicationResult<Visit>.Failure(ApplicationErrors.Required(nameof(userId)));
        }

        if (expectedVersion < 1)
        {
            return ApplicationResult<Visit>.Failure(
                PassportApplicationErrors.InvalidVisitVersion());
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(visitIdValue);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await visitRepository.GetOwnedAsync(
            visitId,
            userId.Trim(),
            cancellationToken);
        if (visit is null)
        {
            return ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitNotFound());
        }

        return visit.Version == expectedVersion
            ? ApplicationResult<Visit>.Success(visit)
            : ApplicationResult<Visit>.Failure(PassportApplicationErrors.VisitConcurrencyConflict());
    }

    public static async Task<ApplicationResult<VisitResult>> ChangeStatusAsync(
        string userId,
        string visitId,
        long expectedVersion,
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IPassportPendingMutationReconciler? pendingMutationReconciler,
        Action<Visit, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        ApplicationResult<Visit> loaded = await LoadAsync(
            userId,
            visitId,
            expectedVersion,
            visitRepository,
            cancellationToken);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return ApplicationResult<VisitResult>.Failure(loaded.Errors);
        }

        Visit visit = loaded.Value;
        IVisitContentMutationLease? contentMutationLease = null;
        if (visit.Status == VisitStatus.Draft
            && pendingMutationReconciler is not null)
        {
            contentMutationLease =
                await pendingMutationReconciler.TryAcquireReconciledLifecycleLeaseAsync(
                    visit,
                    cancellationToken);
        }

        if (visit.Status == VisitStatus.Draft
            && pendingMutationReconciler is not null
            && contentMutationLease is null)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.VisitConcurrencyConflict());
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;
        using CancellationTokenSource? leaseCancellationSource =
            PassportLeaseCancellation.Link(contentMutationLease, cancellationToken);
        CancellationToken guardedCancellationToken =
            leaseCancellationSource?.Token ?? cancellationToken;
        VisitStatus previousStatus = visit.Status;
        try
        {
            mutation(visit, clock.UtcNow);
        }
        catch (VisitValidationException exception)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.InvalidVisit(
                        exception.ErrorCode,
                        exception.Message)));
        }
        catch (TimeZoneNotFoundException)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.InvalidTimeZone()));
        }
        catch (InvalidTimeZoneException)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.InvalidTimeZone()));
        }

        PassportAuditEvent auditEvent = PassportVisitAuditEventFactory.VisitStatusChanged(
            visit,
            previousStatus);
        bool updated = contentMutationLease is null
            ? await visitRepository.TryUpdateOwnedAuditedAsync(
                visit,
                expectedVersion,
                auditEvent,
                guardedCancellationToken)
            : await visitRepository.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                expectedVersion,
                auditEvent,
                contentMutationLease.Token,
                guardedCancellationToken);
        if (!updated)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                ApplicationResult<VisitResult>.Failure(
                    PassportApplicationErrors.VisitConcurrencyConflict()));
        }

        await PassportAuditDelivery.PublishAsync(
            auditPublisher,
            auditEvent,
            guardedCancellationToken);
        return PassportContentMutationLeaseCompletion.Complete(
            contentMutationLease,
            ApplicationResult<VisitResult>.Success(
                PassportVisitResultFactory.Create(visit)));
    }
}
