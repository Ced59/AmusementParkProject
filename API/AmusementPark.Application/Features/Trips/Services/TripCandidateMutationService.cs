using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripCandidateMutationService
{
    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly IParkRepository parkRepository;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripCandidateMutationService(
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

    public Task<ApplicationResult<TripParkCandidateResult>> UpdateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        TripParkCandidateDetailsInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        return this.MutateAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            candidateId,
            expectedCandidateVersion,
            (trip, candidate) =>
            {
                TripProgramRules.ValidateCandidateDates(trip.DateProposal, input.CandidateDates);
                candidate.UpdateDetails(
                    input.CandidateDates,
                    input.CollectiveNote,
                    candidate.FitSnapshot,
                    this.NowUtc());
            },
            (candidate, token) => this.ValidateDatesAgainstDaysAsync(
                candidate,
                input.CandidateDates,
                token),
            cancellationToken);
    }

    public Task<ApplicationResult<TripParkCandidateResult>> ChangeStateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        TripParkCandidateState state,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            candidateId,
            expectedCandidateVersion,
            (_, candidate) => candidate.ChangeState(state, this.NowUtc()),
            (candidate, token) => this.ValidateStateAgainstDaysAsync(candidate, state, token),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripParkCandidateResult>> MutateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        Action<TripPlan, TripParkCandidate> mutation,
        Func<TripParkCandidate, CancellationToken, Task<ApplicationError?>> consistencyGuard,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveEditableTripAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null
            || !TripParkCandidateId.TryParse(candidateId, out TripParkCandidateId parsedCandidateId))
        {
            return resolved.IsSuccess
                ? ApplicationResult<TripParkCandidateResult>.Failure(TripPlanApplicationErrors.CandidateNotFound())
                : ApplicationResult<TripParkCandidateResult>.Failure(resolved.Errors);
        }

        TripPlan trip = resolved.Value;
        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            userId.Trim(),
            TripPermission.EditProgram,
            Guid.NewGuid().ToString("N"),
            async lease =>
            {
                TripParkCandidate? candidate = await this.candidateRepository.GetAsync(
                    trip.Id,
                    parsedCandidateId,
                    cancellationToken);
                if (candidate is null)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(
                        TripPlanApplicationErrors.CandidateNotFound());
                }

                if (candidate.Version != expectedCandidateVersion)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(
                        TripPlanApplicationErrors.ChangedConcurrently(candidate.Version));
                }

                ApplicationError? consistencyError = await consistencyGuard(candidate, cancellationToken);
                if (consistencyError is not null)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(consistencyError);
                }

                try
                {
                    mutation(trip, candidate);
                }
                catch (TripPlanValidationException exception)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(
                        TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
                }

                if (candidate.Version == expectedCandidateVersion)
                {
                    Park? unchangedPark = await this.parkRepository.GetByIdAsync(
                        candidate.ParkId,
                        true,
                        cancellationToken);
                    return ApplicationResult<TripParkCandidateResult>.Success(
                        TripProgramResultFactory.ToCandidateResult(candidate, unchangedPark));
                }

                TripActivityWrite? pendingActivity = this.activityRecorder?.CreateWrite(
                    trip,
                    userId.Trim(),
                    TripActivityKind.CandidateUpdated,
                    $"candidate-update:{lease.OperationId}",
                    1);
                TripParkCandidateWriteResult outcome = await this.candidateRepository.ReplaceAsync(
                    candidate,
                    expectedCandidateVersion,
                    lease,
                    pendingActivity,
                    cancellationToken);
                if (outcome.Outcome != TripChildWriteOutcome.Success || outcome.Candidate is null)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(
                        outcome.Outcome == TripChildWriteOutcome.NotFound
                            ? TripPlanApplicationErrors.CandidateNotFound()
                            : TripPlanApplicationErrors.ChangedConcurrently(outcome.CurrentVersion));
                }

                if (this.activityRecorder is not null)
                {
                    await this.activityRecorder.RecordAsync(
                        trip,
                        userId.Trim(),
                        TripActivityKind.CandidateUpdated,
                        $"candidate-update:{lease.OperationId}",
                        1,
                        CancellationToken.None);
                }

                Park? park = await this.parkRepository.GetByIdAsync(
                    outcome.Candidate.ParkId,
                    true,
                    cancellationToken);
                return ApplicationResult<TripParkCandidateResult>.Success(
                    TripProgramResultFactory.ToCandidateResult(outcome.Candidate, park));
            },
            cancellationToken);
    }

    private async Task<ApplicationError?> ValidateDatesAgainstDaysAsync(
        TripParkCandidate candidate,
        IReadOnlyCollection<DateOnly> candidateDates,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripDayPlan> days = await this.dayPlanRepository.ListAsync(
            candidate.TripPlanId,
            cancellationToken);
        try
        {
            TripProgramRules.ValidateCandidateDatesAgainstDays(candidate, candidateDates, days);
            return null;
        }
        catch (TripPlanValidationException)
        {
            return TripPlanApplicationErrors.CandidateChangeInvalidatesDay();
        }
    }

    private async Task<ApplicationError?> ValidateStateAgainstDaysAsync(
        TripParkCandidate candidate,
        TripParkCandidateState state,
        CancellationToken cancellationToken)
    {
        if (state == TripParkCandidateState.Selected)
        {
            return null;
        }

        IReadOnlyCollection<TripDayPlan> days = await this.dayPlanRepository.ListAsync(
            candidate.TripPlanId,
            cancellationToken);
        try
        {
            TripProgramRules.ValidateCandidateStateAgainstDays(candidate, state, days);
            return null;
        }
        catch (TripPlanValidationException)
        {
            return TripPlanApplicationErrors.CandidateChangeInvalidatesDay();
        }
    }

    private async Task<ApplicationResult<TripPlan>> ResolveEditableTripAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion < 1
            || !TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return Invalid("A valid trip and version are required.");
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

    private DateTime NowUtc()
    {
        return this.timeProvider.GetUtcNow().UtcDateTime;
    }

    private static bool TryNormalizeIdentity(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedTripId)
    {
        normalizedUserId = string.Empty;
        parsedTripId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        return TripPlanId.TryParse(tripPlanId, out parsedTripId);
    }

    private static ApplicationResult<TripPlan> Invalid(string message)
    {
        return ApplicationResult<TripPlan>.Failure(TripPlanApplicationErrors.Invalid(
            TripPlanErrorCodes.InvalidState,
            message));
    }
}
