using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripDayProgramService
{
    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly IParkRepository parkRepository;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripDayProgramService(
        ITripPlanRepository tripPlanRepository,
        ITripParkCandidateRepository candidateRepository,
        ITripDayPlanRepository dayPlanRepository,
        IParkRepository parkRepository,
        TripChildMutationExecutor mutationExecutor,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.tripPlanRepository = tripPlanRepository ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.candidateRepository = candidateRepository ?? throw new ArgumentNullException(nameof(candidateRepository));
        this.dayPlanRepository = dayPlanRepository ?? throw new ArgumentNullException(nameof(dayPlanRepository));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.mutationExecutor = mutationExecutor ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<TripDayPlanResult>> PutAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        DateOnly localDate,
        long? expectedDayVersion,
        TripDayPlanInput input,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveEditableTripAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null
            || !TripParkCandidateId.TryParse(input.ParkCandidateId, out TripParkCandidateId candidateId))
        {
            return resolved.IsSuccess
                ? ApplicationResult<TripDayPlanResult>.Failure(TripPlanApplicationErrors.CandidateNotFound())
                : ApplicationResult<TripDayPlanResult>.Failure(resolved.Errors);
        }

        TripPlan trip = resolved.Value;
        string normalizedUserId = userId.Trim();
        string requestHash = TripProgramOperationFingerprint.BuildDayRequestHash(localDate, input);
        string operationId = TripProgramOperationFingerprint.BuildOperationId(
            normalizedUserId,
            trip.Id,
            requestHash);
        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            normalizedUserId,
            TripPermission.EditProgram,
            operationId,
            lease => this.PutUnderLeaseAsync(
                trip,
                normalizedUserId,
                candidateId,
                localDate,
                expectedDayVersion,
                input,
                lease,
                requestHash,
                cancellationToken),
            cancellationToken);
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        DateOnly localDate,
        long expectedDayVersion,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveEditableTripAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return ApplicationResult.Failure(resolved.Errors);
        }

        TripPlan trip = resolved.Value;
        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            userId.Trim(),
            TripPermission.EditProgram,
            Guid.NewGuid().ToString("N"),
            async lease =>
            {
                TripDayPlanWriteResult result = await this.dayPlanRepository.DeleteAsync(
                    trip.Id,
                    localDate,
                    expectedDayVersion,
                    lease,
                    cancellationToken);
                if (result.Outcome == TripChildWriteOutcome.Success
                    && this.activityRecorder is not null)
                {
                    await this.activityRecorder.RecordAsync(
                        trip,
                        userId.Trim(),
                        TripActivityKind.DayRemoved,
                        $"day-remove:{lease.OperationId}",
                        1,
                        CancellationToken.None);
                }

                return result.Outcome switch
                {
                    TripChildWriteOutcome.Success => ApplicationResult.Success(),
                    TripChildWriteOutcome.NotFound =>
                        ApplicationResult.Failure(TripPlanApplicationErrors.DayNotFound()),
                    _ => ApplicationResult.Failure(
                        TripPlanApplicationErrors.ChangedConcurrently(result.CurrentVersion)),
                };
            },
            cancellationToken);
    }

    private async Task<ApplicationResult<TripDayPlanResult>> PutUnderLeaseAsync(
        TripPlan trip,
        string actorUserId,
        TripParkCandidateId candidateId,
        DateOnly localDate,
        long? expectedDayVersion,
        TripDayPlanInput input,
        TripChildMutationLease lease,
        string requestHash,
        CancellationToken cancellationToken)
    {
        TripParkCandidate? candidate = await this.candidateRepository.GetAsync(
            trip.Id,
            candidateId,
            cancellationToken);
        if (candidate is null)
        {
            return ApplicationResult<TripDayPlanResult>.Failure(
                TripPlanApplicationErrors.CandidateNotFound());
        }

        try
        {
            TripProgramRules.ValidateDayDate(trip.DateProposal, localDate);
            TripProgramRules.ValidateDayCandidate(candidate, localDate);
            TripDayBlock[] blocks = input.Blocks.Select(static block => new TripDayBlock(
                TripDayBlockId.Parse(block.BlockId),
                block.Type,
                block.Title,
                block.Details,
                block.LocalTime,
                block.SortPosition)).ToArray();
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            IReadOnlyCollection<TripDayPlan> days = await this.dayPlanRepository.ListAsync(
                trip.Id,
                cancellationToken);
            TripDayPlan? existing = days.SingleOrDefault(day => day.LocalDate == localDate);
            ApplicationResult<TripDayPlan>? replay = ResolveReplay(
                existing,
                candidate,
                expectedDayVersion,
                input,
                blocks,
                nowUtc);
            if (replay is not null)
            {
                return replay.IsSuccess && replay.Value is not null
                    ? ApplicationResult<TripDayPlanResult>.Success(
                        await this.ToResultAsync(replay.Value, cancellationToken))
                    : ApplicationResult<TripDayPlanResult>.Failure(replay.Errors);
            }

            TripDayPlan dayPlan = existing ?? TripDayPlan.Create(
                TripDayPlanId.New(),
                trip.Id,
                localDate,
                candidate.Id,
                candidate.ParkId,
                input.DesiredArrivalTime,
                input.GroupNote,
                blocks,
                nowUtc);
            if (existing is not null)
            {
                existing.Update(
                    candidate.Id,
                    candidate.ParkId,
                    input.DesiredArrivalTime,
                    input.GroupNote,
                    blocks,
                    nowUtc);
            }

            TripDayPlanWriteResult outcome = await this.dayPlanRepository.PutAsync(
                dayPlan,
                expectedDayVersion,
                lease,
                requestHash,
                cancellationToken);
            if (outcome.Outcome != TripChildWriteOutcome.Success || outcome.DayPlan is null)
            {
                return ApplicationResult<TripDayPlanResult>.Failure(
                    outcome.Outcome == TripChildWriteOutcome.Conflict
                        ? TripPlanApplicationErrors.ChangedConcurrently(outcome.CurrentVersion)
                        : TripPlanApplicationErrors.ChildMutationUnavailable());
            }

            if (this.activityRecorder is not null)
            {
                await this.activityRecorder.RecordAsync(
                    trip,
                    actorUserId,
                    TripActivityKind.DayUpdated,
                    $"day-put:{lease.OperationId}",
                    1,
                    CancellationToken.None);
            }

            return ApplicationResult<TripDayPlanResult>.Success(
                await this.ToResultAsync(outcome.DayPlan, cancellationToken));
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult<TripDayPlanResult>.Failure(
                TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
        }
        catch (ArgumentException exception)
        {
            return ApplicationResult<TripDayPlanResult>.Failure(
                TripPlanApplicationErrors.Invalid(
                    TripPlanErrorCodes.InvalidDayPlan,
                    exception.Message));
        }
    }

    private static ApplicationResult<TripDayPlan>? ResolveReplay(
        TripDayPlan? existing,
        TripParkCandidate candidate,
        long? expectedDayVersion,
        TripDayPlanInput input,
        IReadOnlyCollection<TripDayBlock> blocks,
        DateTime nowUtc)
    {
        if (existing is null)
        {
            return expectedDayVersion.HasValue
                ? ApplicationResult<TripDayPlan>.Failure(TripPlanApplicationErrors.ChildMutationUnavailable())
                : null;
        }

        if (expectedDayVersion.HasValue)
        {
            if (existing.Version == expectedDayVersion.Value)
            {
                return null;
            }

            if (expectedDayVersion.Value < long.MaxValue
                && existing.Version == expectedDayVersion.Value + 1)
            {
                long persistedVersion = existing.Version;
                existing.Update(
                    candidate.Id,
                    candidate.ParkId,
                    input.DesiredArrivalTime,
                    input.GroupNote,
                    blocks,
                    nowUtc);
                if (existing.Version == persistedVersion)
                {
                    return ApplicationResult<TripDayPlan>.Success(existing);
                }

                return ApplicationResult<TripDayPlan>.Failure(
                    TripPlanApplicationErrors.ChangedConcurrently(persistedVersion));
            }

            return ApplicationResult<TripDayPlan>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(existing.Version));
        }

        long unchangedVersion = existing.Version;
        existing.Update(
            candidate.Id,
            candidate.ParkId,
            input.DesiredArrivalTime,
            input.GroupNote,
            blocks,
            nowUtc);
        return existing.Version == unchangedVersion
            ? ApplicationResult<TripDayPlan>.Success(existing)
            : ApplicationResult<TripDayPlan>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(unchangedVersion));
    }

    private async Task<TripDayPlanResult> ToResultAsync(
        TripDayPlan dayPlan,
        CancellationToken cancellationToken)
    {
        Park? park = await this.parkRepository.GetByIdAsync(
            dayPlan.ParkId,
            true,
            cancellationToken);
        return TripProgramResultFactory.ToDayResult(dayPlan, park);
    }

    private async Task<ApplicationResult<TripPlan>> ResolveEditableTripAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string normalizedUserId;
        if (expectedVersion < 1 || !TripPlanId.TryParse(tripPlanId, out TripPlanId parsedTripId))
        {
            return InvalidTrip();
        }

        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return InvalidTrip();
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        TripEffectiveRole? role = trip?.ResolveRole(normalizedUserId);
        if (trip is null
            || !role.HasValue
            || !TripAuthorizationPolicy.HasPermission(role.Value, TripPermission.EditProgram))
        {
            return ApplicationResult<TripPlan>.Failure(TripPlanApplicationErrors.NotFound());
        }

        return trip.Version == expectedVersion
            ? ApplicationResult<TripPlan>.Success(trip)
            : ApplicationResult<TripPlan>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
    }

    private static ApplicationResult<TripPlan> InvalidTrip()
    {
        return ApplicationResult<TripPlan>.Failure(TripPlanApplicationErrors.Invalid(
            TripPlanErrorCodes.InvalidState,
            "A valid trip and version are required."));
    }
}
