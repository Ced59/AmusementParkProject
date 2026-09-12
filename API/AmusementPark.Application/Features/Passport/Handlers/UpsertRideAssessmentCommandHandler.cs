using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class UpsertRideAssessmentCommandHandler
    : ICommandHandler<UpsertRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository? visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal UpsertRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(null!, occurrenceRepository, clock, null!, null!)
    {
    }

    public UpsertRideAssessmentCommandHandler(
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
        UpsertRideAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedRideAssessmentScope? scope = ParseScope(command.UserId, command.OccurrenceId);
        if (scope is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessmentVersion());
        }

        RatingValue value;
        try
        {
            value = RatingValue.FromDouble(command.Value);
        }
        catch (RatingValueValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessment(
                exception.ErrorCode,
                exception.Message));
        }

        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedByIdAsync(
            scope.OccurrenceId,
            scope.UserId,
            cancellationToken);
        if (occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        EditableVisitValidation editableVisit = await LoadEditableVisitAsync(
            occurrence,
            scope.UserId,
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
            occurrence.UpsertAssessment(value, command.PrivateComment, this.clock.UtcNow);
        }
        catch (RideAssessmentValidationException exception)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.InvalidRideAssessment(
                    exception.ErrorCode,
                    exception.Message)));
        }
        catch (RideOccurrenceValidationException exception)
        {
            return PassportContentMutationLeaseCompletion.Complete(
                contentMutationLease,
                Failure(PassportApplicationErrors.InvalidRideOccurrence(
                    exception.ErrorCode,
                    exception.Message)));
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideAssessmentUpserted(
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

    private static ParsedRideAssessmentScope? ParseScope(string? userId, string? occurrenceId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            return new ParsedRideAssessmentScope(userId.Trim(), RideOccurrenceId.Parse(occurrenceId));
        }
        catch (ArgumentException)
        {
            return null;
        }
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

    internal static async Task<EditableVisitValidation> LoadEditableVisitAsync(
        RideOccurrence occurrence,
        string userId,
        IUserVisitRepository? visitRepository,
        CancellationToken cancellationToken)
    {
        if (visitRepository is null)
        {
            return new EditableVisitValidation(null, null);
        }

        Visit? visit = await visitRepository.GetOwnedAsync(
            occurrence.VisitId,
            userId,
            cancellationToken);
        return visit is null
            ? new EditableVisitValidation(null, PassportApplicationErrors.VisitNotFound())
            : new EditableVisitValidation(
                visit,
                PassportRideOccurrenceHandlerSupport.ValidateEditable(visit));
    }

}
