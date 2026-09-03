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

    internal UpsertRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(null!, occurrenceRepository, clock, null!)
    {
    }

    public UpsertRideAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
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

        ApplicationError? editableError = await ValidateVisitEditableAsync(
            occurrence,
            scope.UserId,
            this.visitRepository,
            cancellationToken);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        RideOccurrenceAuditSnapshot previous = RideOccurrenceAuditSnapshot.Capture(occurrence);
        try
        {
            occurrence.UpsertAssessment(value, command.PrivateComment, this.clock.UtcNow);
        }
        catch (RideAssessmentValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideAssessment(
                exception.ErrorCode,
                exception.Message));
        }
        catch (RideOccurrenceValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                exception.ErrorCode,
                exception.Message));
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideAssessmentUpserted(
                occurrence,
                previous);
        bool updated = auditEvent is null
            ? await this.occurrenceRepository.TryUpdateOwnedAsync(
                occurrence,
                expectedVersion,
                cancellationToken)
            : await this.occurrenceRepository.TryUpdateOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                cancellationToken);
        if (!updated)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return Success(occurrence);
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

    internal static async Task<ApplicationError?> ValidateVisitEditableAsync(
        RideOccurrence occurrence,
        string userId,
        IUserVisitRepository? visitRepository,
        CancellationToken cancellationToken)
    {
        if (visitRepository is null)
        {
            return null;
        }

        Visit? visit = await visitRepository.GetOwnedAsync(
            occurrence.VisitId,
            userId,
            cancellationToken);
        return visit is null
            ? PassportApplicationErrors.VisitNotFound()
            : PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
    }

    private sealed record ParsedRideAssessmentScope(string UserId, RideOccurrenceId OccurrenceId);
}

public sealed class DeleteRideAssessmentCommandHandler
    : ICommandHandler<DeleteRideAssessmentCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository? visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;

    internal DeleteRideAssessmentCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(null!, occurrenceRepository, clock, null!)
    {
    }

    public DeleteRideAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
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

        ApplicationError? editableError = await UpsertRideAssessmentCommandHandler.ValidateVisitEditableAsync(
            occurrence,
            userId,
            this.visitRepository,
            cancellationToken);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        RideOccurrenceAuditSnapshot previous = RideOccurrenceAuditSnapshot.Capture(occurrence);
        try
        {
            occurrence.DeleteAssessment(this.clock.UtcNow);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                exception.ErrorCode,
                exception.Message));
        }

        if (occurrence.Version == expectedVersion)
        {
            bool versionIsCurrent = await this.occurrenceRepository.TryConfirmOwnedVersionAsync(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.UserId,
                expectedVersion,
                cancellationToken);
            return versionIsCurrent
                ? Success(occurrence)
                : Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideAssessmentDeleted(
                occurrence,
                previous);
        bool updated = auditEvent is null
            ? await this.occurrenceRepository.TryUpdateOwnedAsync(
                occurrence,
                expectedVersion,
                cancellationToken)
            : await this.occurrenceRepository.TryUpdateOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                cancellationToken);
        if (!updated)
        {
            return Failure(PassportApplicationErrors.RideAssessmentConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return Success(occurrence);
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
