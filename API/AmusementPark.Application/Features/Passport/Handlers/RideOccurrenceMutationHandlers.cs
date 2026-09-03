using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class UpdateRideOccurrenceCommandHandler
    : ICommandHandler<UpdateRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal UpdateRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        IPassportClock clock)
        : this(visitRepository, occurrenceRepository, targetResolver, clock, null!, null!)
    {
    }

    public UpdateRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        IPassportClock clock,
        IPassportAuditPublisher auditPublisher,
        IVisitContentMutationLeaseManager contentMutationLeaseManager)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
        this.clock = clock;
        this.auditPublisher = auditPublisher;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
    }

    public async Task<ApplicationResult<RideOccurrenceResult>> HandleAsync(
        UpdateRideOccurrenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            command.UserId,
            command.VisitId,
            command.OccurrenceId);
        if (scope is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        if (command.ExpectedVersion < 1 || !Enum.IsDefined(command.Status))
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrenceUpdate());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedAsync(
            scope.OccurrenceId,
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (visit is null || occurrence is null)
        {
            return Failure(occurrence is null
                ? PassportApplicationErrors.RideOccurrenceNotFound()
                : PassportApplicationErrors.VisitNotFound());
        }

        ApplicationError? editableError = PassportRideOccurrenceHandlerSupport.ValidateEditable(visit);
        if (editableError is not null)
        {
            return Failure(editableError);
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        IReadOnlyDictionary<string, VisitTarget> targets = await this.targetResolver.ResolveAsync(
            new[] { occurrence.ParkItemId },
            cancellationToken);
        if (!targets.TryGetValue(occurrence.ParkItemId, out VisitTarget? target))
        {
            return Failure(PassportApplicationErrors.VisitTargetNotFound());
        }

        if (!string.Equals(target.ParkId, visit.ParkId, StringComparison.Ordinal))
        {
            return Failure(PassportApplicationErrors.VisitTargetParkMismatch());
        }

        if (target.Category != ParkItemCategory.Attraction)
        {
            return Failure(PassportApplicationErrors.VisitTargetNotAttraction());
        }

        HistoricalConsistency consistency =
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                visit.Date,
                target.OpeningDate,
                target.ClosingDate);
        if (consistency == HistoricalConsistency.ConfirmedConflict
            && !command.ConfirmHistoricalConflict)
        {
            return Failure(PassportApplicationErrors.HistoricalConflictConfirmationRequired());
        }

        IVisitContentMutationLease? contentMutationLease =
            this.contentMutationLeaseManager is null
                ? null
                : await this.contentMutationLeaseManager.TryAcquireAsync(
                    visit,
                    this.clock.UtcNow,
                    cancellationToken);
        if (this.contentMutationLeaseManager is not null && contentMutationLease is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;

        long expectedVersion = occurrence.Version;
        RideOccurrenceAuditSnapshot previous = RideOccurrenceAuditSnapshot.Capture(occurrence);
        try
        {
            occurrence.Update(
                visit,
                new OccurrenceMoment(command.LocalTime, command.IsApproximate),
                command.Status,
                consistency,
                null,
                command.PrivateNote,
                this.clock.UtcNow);
        }
        catch (RideOccurrenceValidationException exception)
        {
            return Failure(PassportApplicationErrors.InvalidRideOccurrence(
                exception.ErrorCode,
                exception.Message));
        }

        if (occurrence.Version == expectedVersion)
        {
            bool versionIsCurrent =
                await this.occurrenceRepository.TryConfirmOwnedVersionAsync(
                    occurrence.Id,
                    occurrence.VisitId,
                    occurrence.UserId,
                    expectedVersion,
                    cancellationToken);
            return versionIsCurrent
                ? Success(occurrence)
                : Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideOccurrenceChanged(
                occurrence,
                previous,
                $"{occurrence.Id.Value}:{occurrence.Version}:update");
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
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return Success(occurrence);
    }

    private static ApplicationResult<RideOccurrenceResult> Success(
        RideOccurrence occurrence)
    {
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }
}

public sealed class DeleteRideOccurrenceCommandHandler
    : ICommandHandler<DeleteRideOccurrenceCommand, ApplicationResult<RideOccurrenceResult>>
{
    private readonly IUserVisitRepository? visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;
    private readonly IPassportAuditPublisher? auditPublisher;
    private readonly IVisitContentMutationLeaseManager? contentMutationLeaseManager;

    internal DeleteRideOccurrenceCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
        : this(null!, occurrenceRepository, clock, null!, null!)
    {
    }

    public DeleteRideOccurrenceCommandHandler(
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
        DeleteRideOccurrenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ParsedOccurrenceScope? scope = PassportRideOccurrenceHandlerSupport.ParseOccurrenceScope(
            command.UserId,
            command.VisitId,
            command.OccurrenceId);
        if (scope is null || command.ExpectedVersion < 1)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        RideOccurrence? occurrence = await this.occurrenceRepository.GetOwnedAsync(
            scope.OccurrenceId,
            scope.VisitId,
            scope.UserId,
            cancellationToken);
        if (occurrence is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceNotFound());
        }

        Visit? visit = null;
        if (this.visitRepository is not null)
        {
            visit = await this.visitRepository.GetOwnedAsync(
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
        }

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        IVisitContentMutationLease? contentMutationLease =
            this.contentMutationLeaseManager is null || visit is null
                ? null
                : await this.contentMutationLeaseManager.TryAcquireAsync(
                    visit,
                    this.clock.UtcNow,
                    cancellationToken);
        if (this.contentMutationLeaseManager is not null && contentMutationLease is null)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        await using IVisitContentMutationLease? contentMutationLeaseScope =
            contentMutationLease;

        long expectedVersion = occurrence.Version;
        occurrence.Delete(this.clock.UtcNow);
        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportRideAuditEventFactory.RideOccurrenceDeleted(
                occurrence,
                $"{occurrence.Id.Value}:{occurrence.Version}:delete");
        bool deleted = auditEvent is null
            ? await this.occurrenceRepository.TryDeleteOwnedAsync(
                occurrence,
                expectedVersion,
                cancellationToken)
            : await this.occurrenceRepository.TryDeleteOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                cancellationToken);
        if (!deleted)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        await PassportAuditDelivery.PublishAsync(
            this.auditPublisher,
            auditEvent,
            cancellationToken);
        return ApplicationResult<RideOccurrenceResult>.Success(
            PassportRideOccurrenceResultFactory.Create(occurrence));
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }
}
