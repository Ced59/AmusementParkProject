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

    public UpdateRideOccurrenceCommandHandler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitTargetResolver targetResolver,
        IPassportClock clock)
    {
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.targetResolver = targetResolver;
        this.clock = clock;
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

        long expectedVersion = occurrence.Version;
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
            return Success(occurrence);
        }

        bool updated = await this.occurrenceRepository.TryUpdateOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return updated
            ? Success(occurrence)
            : Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
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
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IPassportClock clock;

    public DeleteRideOccurrenceCommandHandler(
        IRideOccurrenceRepository occurrenceRepository,
        IPassportClock clock)
    {
        this.occurrenceRepository = occurrenceRepository;
        this.clock = clock;
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

        if (occurrence.Version != command.ExpectedVersion)
        {
            return Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
        }

        long expectedVersion = occurrence.Version;
        occurrence.Delete(this.clock.UtcNow);
        bool deleted = await this.occurrenceRepository.TryDeleteOwnedAsync(
            occurrence,
            expectedVersion,
            cancellationToken);
        return deleted
            ? ApplicationResult<RideOccurrenceResult>.Success(
                PassportRideOccurrenceResultFactory.Create(occurrence))
            : Failure(PassportApplicationErrors.RideOccurrenceConcurrencyConflict());
    }

    private static ApplicationResult<RideOccurrenceResult> Failure(ApplicationError error)
    {
        return ApplicationResult<RideOccurrenceResult>.Failure(error);
    }
}
