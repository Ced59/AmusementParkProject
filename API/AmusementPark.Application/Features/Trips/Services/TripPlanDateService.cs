using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPlanDateService
{
    private readonly ITripPlanRepository tripPlanRepository;
    private readonly ITripParkCandidateRepository candidateRepository;
    private readonly ITripDayPlanRepository dayPlanRepository;
    private readonly ITripTimeZoneValidator timeZoneValidator;
    private readonly TripChildMutationExecutor mutationExecutor;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripPlanDateService(
        ITripPlanRepository tripPlanRepository,
        ITripParkCandidateRepository candidateRepository,
        ITripDayPlanRepository dayPlanRepository,
        ITripTimeZoneValidator timeZoneValidator,
        TripChildMutationExecutor mutationExecutor,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.tripPlanRepository = tripPlanRepository
            ?? throw new ArgumentNullException(nameof(tripPlanRepository));
        this.candidateRepository = candidateRepository
            ?? throw new ArgumentNullException(nameof(candidateRepository));
        this.dayPlanRepository = dayPlanRepository
            ?? throw new ArgumentNullException(nameof(dayPlanRepository));
        this.timeZoneValidator = timeZoneValidator
            ?? throw new ArgumentNullException(nameof(timeZoneValidator));
        this.mutationExecutor = mutationExecutor
            ?? throw new ArgumentNullException(nameof(mutationExecutor));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<TripPlanResult>> SetDatesAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        TripPlanDatesInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (expectedVersion < 1
            || !TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedId))
        {
            return Invalid(TripPlanErrorCodes.InvalidVersion, "A valid trip and version are required.");
        }

        if (!string.IsNullOrWhiteSpace(input.DestinationTimeZoneId)
            && !this.timeZoneValidator.IsValidIanaTimeZone(input.DestinationTimeZoneId.Trim()))
        {
            return Invalid(
                TripPlanErrorCodes.InvalidTimeZone,
                "The destination time zone must be a valid IANA identifier.");
        }

        TripPlan? trip = await this.tripPlanRepository.GetAccessibleAsync(
            normalizedUserId,
            parsedId,
            cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound());
        }
        TripEffectiveRole? role = trip.ResolveRole(normalizedUserId);
        if (!role.HasValue || !TripAuthorizationPolicy.HasPermission(role.Value, TripPermission.EditPlan))
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedVersion)
        {
            if (expectedVersion < long.MaxValue && trip.Version == expectedVersion + 1)
            {
                long replayedVersion = trip.Version;
                try
                {
                    trip.SetDates(
                        input.DateProposal,
                        input.DestinationTimeZoneId,
                        this.timeProvider.GetUtcNow().UtcDateTime);
                }
                catch (TripPlanValidationException exception)
                {
                    return Invalid(exception.Code, exception.Message);
                }

                if (trip.Version == replayedVersion)
                {
                    await this.RecordActivityAsync(trip, normalizedUserId);
                    return ApplicationResult<TripPlanResult>.Success(
                        TripPlanResultFactory.ToResult(trip, normalizedUserId));
                }
            }

            return ApplicationResult<TripPlanResult>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        return await this.mutationExecutor.ExecuteAccessibleAsync(
            trip,
            normalizedUserId,
            TripPermission.EditPlan,
            Guid.NewGuid().ToString("N"),
            lease => this.SetDatesUnderLeaseAsync(
                trip,
                normalizedUserId,
                expectedVersion,
                input,
                lease,
                cancellationToken),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripPlanResult>> SetDatesUnderLeaseAsync(
        TripPlan trip,
        string actorUserId,
        long expectedVersion,
        TripPlanDatesInput input,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        Task<IReadOnlyCollection<TripParkCandidate>> candidatesTask =
            this.candidateRepository.ListAsync(trip.Id, cancellationToken);
        Task<IReadOnlyCollection<TripDayPlan>> daysTask =
            this.dayPlanRepository.ListAsync(trip.Id, cancellationToken);
        await Task.WhenAll(candidatesTask, daysTask);
        try
        {
            TripProgramRules.ValidateProgramAgainstProposal(
                input.DateProposal,
                await candidatesTask,
                await daysTask);
            trip.SetDates(
                input.DateProposal,
                input.DestinationTimeZoneId,
                this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return Invalid(exception.Code, exception.Message);
        }

        if (trip.Version == expectedVersion)
        {
            return ApplicationResult<TripPlanResult>.Success(
                TripPlanResultFactory.ToResult(trip, actorUserId));
        }

        TripPlanWriteResult outcome = await this.tripPlanRepository.ReplaceAccessibleUnderChildLeaseAsync(
            actorUserId,
            trip,
            expectedVersion,
            lease,
            cancellationToken);
        if (outcome.Outcome != TripPlanWriteOutcome.Success || outcome.PersistedTripPlan is null)
        {
            return outcome.Outcome == TripPlanWriteOutcome.NotFound
                ? ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound())
                : ApplicationResult<TripPlanResult>.Failure(
                    TripPlanApplicationErrors.ChangedConcurrently(outcome.CurrentVersion));
        }

        if (this.activityRecorder is not null)
        {
            await this.RecordActivityAsync(outcome.PersistedTripPlan, actorUserId);
        }

        return ApplicationResult<TripPlanResult>.Success(
            TripPlanResultFactory.ToResult(outcome.PersistedTripPlan, actorUserId));
    }

    private Task RecordActivityAsync(TripPlan trip, string actorUserId)
    {
        return this.activityRecorder?.RecordAsync(
                trip,
                actorUserId,
                TripActivityKind.DatesChanged,
                TripActivityRecorder.RootOperationKey(
                    TripActivityKind.DatesChanged,
                    trip.Version),
                1,
                CancellationToken.None)
            ?? Task.CompletedTask;
    }

    private static bool TryNormalizeIdentity(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedId)
    {
        normalizedUserId = string.Empty;
        parsedId = default;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        return TripPlanId.TryParse(tripPlanId, out parsedId);
    }

    private static ApplicationResult<TripPlanResult> Invalid(string code, string message)
    {
        return ApplicationResult<TripPlanResult>.Failure(
            TripPlanApplicationErrors.Invalid(code, message));
    }
}
