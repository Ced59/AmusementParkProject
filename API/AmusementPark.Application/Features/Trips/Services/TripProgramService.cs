using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramService
{
    public const int MaximumIdempotencyKeyLength = 200;

    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly IParkRepository parkRepository;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TripProgramResultFactory resultFactory;
    private readonly TimeProvider timeProvider;

    public TripProgramService(
        ITripPlanRepository tripPlanRepository,
        ITripParkCandidateRepository candidateRepository,
        ITripDayPlanRepository dayPlanRepository,
        IParkRepository parkRepository,
        TripChildMutationExecutor mutationExecutor,
        TripProgramResultFactory resultFactory,
        TimeProvider? timeProvider = null)
    {
        this.tripPlanRepository = tripPlanRepository ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.candidateRepository = candidateRepository ?? throw new ArgumentNullException(nameof(candidateRepository));
        this.dayPlanRepository = dayPlanRepository ?? throw new ArgumentNullException(nameof(dayPlanRepository));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.mutationExecutor = mutationExecutor ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripProgramResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return Invalid<TripProgramResult>("A valid trip identifier is required.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripProgramResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        return ApplicationResult<TripProgramResult>.Success(await this.resultFactory.BuildAsync(
            parsedTripId,
            cancellationToken));
    }

    public async Task<ApplicationResult<CreateTripParkCandidateResult>> AddCandidateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string idempotencyKey,
        TripParkCandidateInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        string normalizedKey = idempotencyKey?.Trim() ?? string.Empty;
        if (normalizedKey.Length is 0 or > MaximumIdempotencyKeyLength)
        {
            return Invalid<CreateTripParkCandidateResult>("A bounded idempotency key is required.");
        }

        if (!TryNormalizeIdentity(
                userId,
                tripPlanId,
                out string normalizedUserId,
                out TripPlanId parsedTripId))
        {
            return Invalid<CreateTripParkCandidateResult>("A valid trip identifier is required.");
        }

        string normalizedParkId;
        try
        {
            normalizedParkId = IdentifierRules.NormalizeRequired(input.ParkId, nameof(input.ParkId));
        }
        catch (ArgumentException exception)
        {
            return Invalid<CreateTripParkCandidateResult>(exception.Message);
        }

        string operationId = TripProgramOperationFingerprint.BuildOperationId(
            normalizedUserId,
            parsedTripId,
            normalizedKey);
        string requestHash = TripProgramOperationFingerprint.BuildCandidateRequestHash(
            normalizedParkId,
            input.CandidateDates,
            input.Source,
            input.CollectiveNote);
        TripParkCandidateWriteResult replay = await this.candidateRepository.ResolveCreationAsync(
            parsedTripId,
            operationId,
            requestHash,
            cancellationToken);
        if (replay.Outcome == TripChildWriteOutcome.Success && replay.Candidate is not null)
        {
            Park? replayedPark = await this.parkRepository.GetByIdAsync(
                replay.Candidate.ParkId,
                true,
                cancellationToken);
            return ApplicationResult<CreateTripParkCandidateResult>.Success(
                new CreateTripParkCandidateResult(
                    TripProgramResultFactory.ToCandidateResult(replay.Candidate, replayedPark?.Name),
                    true));
        }

        if (replay.Outcome == TripChildWriteOutcome.IdempotencyConflict)
        {
            return ApplicationResult<CreateTripParkCandidateResult>.Failure(
                TripPlanApplicationErrors.CandidateIdempotencyConflict());
        }

        if (replay.Outcome == TripChildWriteOutcome.Deleted)
        {
            return ApplicationResult<CreateTripParkCandidateResult>.Failure(
                TripPlanApplicationErrors.CandidateCreationWasDeleted());
        }

        ApplicationResult<TripPlan> resolved = await this.ResolveOwnedTripAsync(
            normalizedUserId,
            parsedTripId.Value,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return ApplicationResult<CreateTripParkCandidateResult>.Failure(resolved.Errors);
        }

        TripPlan trip = resolved.Value;
        Park? park = await this.parkRepository.GetByIdAsync(normalizedParkId, false, cancellationToken);
        if (park is null || !park.IsPubliclyDiscoverable())
        {
            return ApplicationResult<CreateTripParkCandidateResult>.Failure(
                TripPlanApplicationErrors.ParkNotAvailable());
        }

        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
            operationId,
            async lease =>
            {
                IReadOnlyCollection<TripParkCandidate> candidates = await this.candidateRepository.ListAsync(
                    trip.Id,
                    cancellationToken);
                bool candidateAlreadyExists = candidates.Any(candidate => string.Equals(
                    candidate.ParkId,
                    park.Id,
                    StringComparison.Ordinal));
                if (!candidateAlreadyExists
                    && candidates.Count >= TripParkCandidate.MaximumCandidatesPerTrip)
                {
                    return ApplicationResult<CreateTripParkCandidateResult>.Failure(
                        TripPlanApplicationErrors.Invalid(
                            TripPlanErrorCodes.ProgramLimitReached,
                            $"A trip cannot contain more than {TripParkCandidate.MaximumCandidatesPerTrip} candidate parks."));
                }

                try
                {
                    TripProgramRules.ValidateCandidateDates(trip.DateProposal, input.CandidateDates);
                    TripMember owner = ResolveOwnerMember(trip);
                    TripParkCandidate candidate = TripParkCandidate.Create(
                        TripParkCandidateId.New(),
                        trip.Id,
                        park.Id,
                        input.CandidateDates,
                        input.Source,
                        input.CollectiveNote,
                        null,
                        owner.Id,
                        TripParkCandidateOrderPlanner.AllocateAppend(
                            candidates.Count == 0 ? null : candidates.Max(static item => item.SortPosition)),
                        this.NowUtc());
                    TripParkCandidateWriteResult written = await this.candidateRepository.CreateAsync(
                        candidate,
                        lease,
                        requestHash,
                        cancellationToken);
                    return written.Outcome switch
                    {
                        TripChildWriteOutcome.Success when written.Candidate is not null =>
                            ApplicationResult<CreateTripParkCandidateResult>.Success(
                                new CreateTripParkCandidateResult(
                                    TripProgramResultFactory.ToCandidateResult(written.Candidate, park.Name),
                                    written.WasReplayed)),
                        TripChildWriteOutcome.Duplicate =>
                            ApplicationResult<CreateTripParkCandidateResult>.Failure(
                                TripPlanApplicationErrors.CandidateAlreadyExists()),
                        TripChildWriteOutcome.IdempotencyConflict =>
                            ApplicationResult<CreateTripParkCandidateResult>.Failure(
                                TripPlanApplicationErrors.CandidateIdempotencyConflict()),
                        _ => ApplicationResult<CreateTripParkCandidateResult>.Failure(
                            TripPlanApplicationErrors.ChildMutationUnavailable()),
                    };
                }
                catch (TripPlanValidationException exception)
                {
                    return ApplicationResult<CreateTripParkCandidateResult>.Failure(
                        TripPlanApplicationErrors.Invalid(exception.Code, exception.Message));
                }
            },
            cancellationToken);
    }

    public Task<ApplicationResult<TripParkCandidateResult>> UpdateCandidateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        TripParkCandidateDetailsInput input,
        CancellationToken cancellationToken)
    {
        return this.MutateCandidateAsync(
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
            (candidate, token) => this.ValidateCandidateDatesAgainstDaysAsync(
                candidate,
                input.CandidateDates,
                token),
            cancellationToken);
    }

    public Task<ApplicationResult<TripParkCandidateResult>> ChangeCandidateStateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        TripParkCandidateState state,
        CancellationToken cancellationToken)
    {
        return this.MutateCandidateAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            candidateId,
            expectedCandidateVersion,
            (_, candidate) => candidate.ChangeState(state, this.NowUtc()),
            (candidate, token) => this.ValidateCandidateStateAgainstDaysAsync(
                candidate,
                state,
                token),
            cancellationToken);
    }

    public async Task<ApplicationResult<TripProgramResult>> MoveCandidateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        string? anchorCandidateId,
        TripParkCandidatePlacement placement,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveOwnedTripAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null
            || !TripParkCandidateId.TryParse(candidateId, out TripParkCandidateId parsedCandidateId))
        {
            return resolved.IsSuccess
                ? ApplicationResult<TripProgramResult>.Failure(TripPlanApplicationErrors.CandidateNotFound())
                : ApplicationResult<TripProgramResult>.Failure(resolved.Errors);
        }

        TripParkCandidateId? parsedAnchorId = null;
        if (anchorCandidateId is not null)
        {
            if (!TripParkCandidateId.TryParse(anchorCandidateId, out TripParkCandidateId anchor))
            {
                return ApplicationResult<TripProgramResult>.Failure(
                    TripPlanApplicationErrors.CandidateNotFound());
            }

            parsedAnchorId = anchor;
        }

        TripPlan trip = resolved.Value;
        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
            Guid.NewGuid().ToString("N"),
            async lease =>
            {
                try
                {
                    IReadOnlyCollection<TripParkCandidate> candidates =
                        await this.candidateRepository.ListAsync(trip.Id, cancellationToken);
                    TripParkCandidateOrderPlan plan = TripParkCandidateOrderPlanner.PlanMove(
                        candidates,
                        parsedCandidateId,
                        parsedAnchorId,
                        placement);
                    TripChildWriteOutcome outcome = await this.candidateRepository.ApplyOrderAsync(
                        trip.Id,
                        plan,
                        lease,
                        this.NowUtc(),
                        cancellationToken);
                    return outcome == TripChildWriteOutcome.Success
                        ? ApplicationResult<TripProgramResult>.Success(await this.resultFactory.BuildAsync(
                            trip.Id,
                            cancellationToken))
                        : ApplicationResult<TripProgramResult>.Failure(
                            TripPlanApplicationErrors.ChildMutationUnavailable());
                }
                catch (KeyNotFoundException)
                {
                    return ApplicationResult<TripProgramResult>.Failure(
                        TripPlanApplicationErrors.CandidateNotFound());
                }
                catch (ArgumentException exception)
                {
                    return Invalid<TripProgramResult>(exception.Message);
                }
            },
            cancellationToken);
    }

    public async Task<ApplicationResult> DeleteCandidateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveOwnedTripAsync(
            userId,
            tripPlanId,
            expectedPlanVersion,
            cancellationToken);
        if (!resolved.IsSuccess || resolved.Value is null)
        {
            return ApplicationResult.Failure(resolved.Errors);
        }

        if (!TripParkCandidateId.TryParse(candidateId, out TripParkCandidateId parsedCandidateId))
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.CandidateNotFound());
        }

        TripPlan trip = resolved.Value;
        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
            Guid.NewGuid().ToString("N"),
            async lease =>
            {
                IReadOnlyCollection<TripDayPlan> days = await this.dayPlanRepository.ListAsync(
                    trip.Id,
                    cancellationToken);
                if (days.Any(day => day.ParkCandidateId == parsedCandidateId))
                {
                    return ApplicationResult.Failure(TripPlanApplicationErrors.CandidateIsUsedByDay());
                }

                TripParkCandidateWriteResult outcome = await this.candidateRepository.DeleteAsync(
                    trip.Id,
                    parsedCandidateId,
                    expectedCandidateVersion,
                    lease,
                    this.NowUtc(),
                    cancellationToken);
                return outcome.Outcome switch
                {
                    TripChildWriteOutcome.Success => ApplicationResult.Success(),
                    TripChildWriteOutcome.NotFound =>
                        ApplicationResult.Failure(TripPlanApplicationErrors.CandidateNotFound()),
                    _ => ApplicationResult.Failure(TripPlanApplicationErrors.ChangedConcurrently(
                        outcome.CurrentVersion)),
                };
            },
            cancellationToken);
    }

    private async Task<ApplicationResult<TripParkCandidateResult>> MutateCandidateAsync(
        string userId,
        string tripPlanId,
        long expectedPlanVersion,
        string candidateId,
        long expectedCandidateVersion,
        Action<TripPlan, TripParkCandidate> mutation,
        Func<TripParkCandidate, CancellationToken, Task<ApplicationError?>> consistencyGuard,
        CancellationToken cancellationToken)
    {
        ApplicationResult<TripPlan> resolved = await this.ResolveOwnedTripAsync(
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
        return await this.mutationExecutor.ExecuteOwnedAsync(
            trip,
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

                ApplicationError? consistencyError = await consistencyGuard(
                    candidate,
                    cancellationToken);
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
                        TripProgramResultFactory.ToCandidateResult(candidate, unchangedPark?.Name));
                }

                TripParkCandidateWriteResult outcome = await this.candidateRepository.ReplaceAsync(
                    candidate,
                    expectedCandidateVersion,
                    lease,
                    cancellationToken);
                if (outcome.Outcome != TripChildWriteOutcome.Success || outcome.Candidate is null)
                {
                    return ApplicationResult<TripParkCandidateResult>.Failure(
                        outcome.Outcome == TripChildWriteOutcome.NotFound
                            ? TripPlanApplicationErrors.CandidateNotFound()
                            : TripPlanApplicationErrors.ChangedConcurrently(outcome.CurrentVersion));
                }

                Park? park = await this.parkRepository.GetByIdAsync(
                    outcome.Candidate.ParkId,
                    true,
                    cancellationToken);
                return ApplicationResult<TripParkCandidateResult>.Success(
                    TripProgramResultFactory.ToCandidateResult(outcome.Candidate, park?.Name));
            },
            cancellationToken);
    }

    private async Task<ApplicationError?> ValidateCandidateDatesAgainstDaysAsync(
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

    private async Task<ApplicationError?> ValidateCandidateStateAgainstDaysAsync(
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

    private async Task<ApplicationResult<TripPlan>> ResolveOwnedTripAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion < 1
            || !TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedTripId))
        {
            return Invalid<TripPlan>("A valid trip and version are required.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetOwnedAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPlan>.Failure(TripPlanApplicationErrors.NotFound());
        }

        return trip.Version == expectedVersion
            ? ApplicationResult<TripPlan>.Success(trip)
            : ApplicationResult<TripPlan>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
    }

    private static TripMember ResolveOwnerMember(TripPlan trip)
    {
        return trip.Members.Single(member => member.State == TripMembershipState.Active
            && string.Equals(member.UserId, trip.OwnerUserId, StringComparison.Ordinal));
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

    private static ApplicationResult<TResult> Invalid<TResult>(string message)
    {
        return ApplicationResult<TResult>.Failure(TripPlanApplicationErrors.Invalid(
            TripPlanErrorCodes.InvalidState,
            message));
    }
}
