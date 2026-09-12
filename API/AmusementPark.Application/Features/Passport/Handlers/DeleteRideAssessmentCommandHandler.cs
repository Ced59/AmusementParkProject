using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class DeleteRideAssessmentCommandHandler
    : ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository? visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal DeleteRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(null!, occurrenceRepository, clock, null!, null!)
    {
    }

    public DeleteRideAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IVisitContentMutationLeaseManager contentMutationLeaseManager)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        DeleteRideAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessmentVersion());
        }

        RideOccurrenceId occurrenceId;
        try
        {
            occurrenceId = RideOccurrenceId.Parse(command.OccurrenceId);
        }
        catch (ArgumentException)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        string userId = command.UserId.Trim();
        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedByIdAsync(
            occurrenceId,
            userId,
            cancellationToken);
        if (occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        EditableVisitValidation editableVisit =
            await UpsertRideAssessmentCommandHandler.LoadEditableVisitAsync(
            occurrence,
            userId,
            this.visitRepository,
            cancellationToken);
        if (editableVisit.Error is not null)
        {
            return Failure(editableVisit.Error);
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        IVisitContentMutationLease? contentMutationLease =
            this.contentMutationLeaseManager is null || editableVisit.Visit is null
                ? null
                : await this.contentMutationLeaseManager.TryAcquireAsync(
                    editableVisit.Visit,
                    this.clock.UtcNow,
                    cancellationToken);
        if (this.contentMutationLeaseManager is not null && contentMutationLease is null)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;
        using CancellationTokenSource? leaseCancellationSource =
            PassportLeaseCancellation.Link(contentMutationLease, cancellationToken);
        CancellationToken guardedCancellationToken =
            leaseCancellationSource?.Token ?? cancellationToken;

        long expectedVersion = occurrence.Version;
        RideOccurrenceAuditSnapshot previous = RideOccurrenceAuditSnapshot.Capture(occurrence);
        try
        {
            occurrence.DeleteAssessment(this.clock.UtcNow);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message)));
        }

        if (occurrence.Version == expectedVersion)
        {
            bool versionIsCurrent = await RideOccurrenceFencedPersistence.TryConfirmAsync(
                this.occurrenceRepository,
                occurrence,
                expectedVersion,
                contentMutationLease,
                guardedCancellationToken);
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                versionIsCurrent
                    ? Success(occurrence)
                    : Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict()));
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideAssessmentDeleted(
                occurrence,
                previous);
        bool updated = await RideOccurrenceFencedPersistence.TryUpdateAsync(
            this.occurrenceRepository,
            occurrence,
            expectedVersion,
            auditEvent,
            contentMutationLease,
            guardedCancellationToken);
        if (!updated)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict()));
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            guardedCancellationToken);
        return PassportContentMutationLeaseCompletion.Complete(
            contentMutationLease,
            Success(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Success(RideOccurrence occurrence)
    {
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }
}
