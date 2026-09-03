using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class UpsertVisitParkAssessmentCommandHandler
    : ICommandHandler<UpsertVisitParkAssessmentCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;

    internal UpsertVisitParkAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock)
        : this(visitRepository, clock, null!)
    {
    }

    public UpsertVisitParkAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
    }

    public async Task<ApplicationResult<VisitResult>> HandleAsync(
        UpsertVisitParkAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedVisitAssessmentScope? scope = ParseScope(command.UserId, command.VisitId);
        if (scope is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidVisitParkAssessmentVersion());
        }

        RatingValue value;
        try
        {
            value = RatingValue.FromDouble(command.Value);
        }
        catch (RatingValueValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidVisitParkAssessment(
                exception.ErrorCode,
                exception.Message));
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        ApplicationError? editableError = PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        if (visit.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.VisitParkAssessmentConcurrencyConflict());
        }

        long expectedVersion = visit.Version;
        VisitParkAssessmentAuditSnapshot? previousAssessment =
            VisitParkAssessmentAuditSnapshot.Capture(visit);
        try
        {
            visit.UpsertParkAssessment(value, command.PrivateComment, this.clock.UtcNow);
        }
        catch (VisitParkAssessmentValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidVisitParkAssessment(
                exception.ErrorCode,
                exception.Message));
        }
        catch (VisitValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidVisit(
                exception.ErrorCode,
                exception.Message));
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportVisitAuditEventFactory.ParkAssessmentUpserted(
                visit,
                previousAssessment);
        bool updated = auditEvent is null
            ? await this.visitRepository.TryUpdateOwnedAsync(
                visit,
                expectedVersion,
                cancellationToken)
            : await this.visitRepository.TryUpdateOwnedAuditedAsync(
                visit,
                expectedVersion,
                auditEvent,
                cancellationToken);
        if (!updated)
        {
            return Failure(PassportApplicationErrors.VisitParkAssessmentConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return Success(visit);
    }

    private static ApplicationResult<VisitResult> Success(Visit visit)
    {
        return ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
    }

    private static ApplicationResult<VisitResult> Failure(ApplicationError error)
    {
        return ApplicationResult<VisitResult>.Failure(error);
    }

    private static ParsedVisitAssessmentScope? ParseScope(string? userId, string? visitId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        try
        {
            return new ParsedVisitAssessmentScope(userId.Trim(), VisitId.Parse(visitId));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private sealed record ParsedVisitAssessmentScope(string UserId, VisitId VisitId);
}

public sealed class DeleteVisitParkAssessmentCommandHandler
    : ICommandHandler<DeleteVisitParkAssessmentCommand, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;

    internal DeleteVisitParkAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock)
        : this(visitRepository, clock, null!)
    {
    }

    public DeleteVisitParkAssessmentCommandHandler(
        IUserVisitRepository visitRepository,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
    }

    public async Task<ApplicationResult<VisitResult>> HandleAsync(
        DeleteVisitParkAssessmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        if (command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.InvalidVisitParkAssessmentVersion());
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(command.VisitId);
        }
        catch (ArgumentException)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        string userId = command.UserId.Trim();
        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            userId,
            cancellationToken);
        if (visit is null)
        {
            return Failure(PassportApplicationErrors.VisitNotFound());
        }

        ApplicationError? editableError = PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        if (visit.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.VisitParkAssessmentConcurrencyConflict());
        }

        long expectedVersion = visit.Version;
        VisitParkAssessmentAuditSnapshot? previousAssessment =
            VisitParkAssessmentAuditSnapshot.Capture(visit);
        try
        {
            visit.DeleteParkAssessment(this.clock.UtcNow);
        }
        catch (VisitValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidVisit(
                exception.ErrorCode,
                exception.Message));
        }

        if (visit.Version == expectedVersion)
        {
            bool versionIsCurrent = await this.visitRepository.TryConfirmOwnedVersionAsync(
                visit.Id,
                visit.UserId,
                expectedVersion,
                cancellationToken);
            return versionIsCurrent
                ? Success(visit)
                : Failure(PassportApplicationErrors.VisitParkAssessmentConcurrencyConflict());
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportVisitAuditEventFactory.ParkAssessmentDeleted(
                visit,
                previousAssessment!);
        bool updated = auditEvent is null
            ? await this.visitRepository.TryUpdateOwnedAsync(
                visit,
                expectedVersion,
                cancellationToken)
            : await this.visitRepository.TryUpdateOwnedAuditedAsync(
                visit,
                expectedVersion,
                auditEvent,
                cancellationToken);
        if (!updated)
        {
            return Failure(PassportApplicationErrors.VisitParkAssessmentConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return Success(visit);
    }

    private static ApplicationResult<VisitResult> Success(Visit visit)
    {
        return ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
    }

    private static ApplicationResult<VisitResult> Failure(ApplicationError error)
    {
        return ApplicationResult<VisitResult>.Failure(error);
    }
}
