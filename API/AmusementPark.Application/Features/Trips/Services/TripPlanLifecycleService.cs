using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPlanLifecycleService
{
    public const int MaximumIdempotencyKeyLength = 200;
    public static readonly TimeSpan MaximumRecentAuthenticationAge = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AuthenticationClockSkew = TimeSpan.FromMinutes(1);

    private readonly ITripPlanRepository repository;
    private readonly ITripTimeZoneValidator timeZoneValidator;
    private readonly TimeProvider timeProvider;
    private readonly TripActivityRecorder? activityRecorder;

    public TripPlanLifecycleService(
        ITripPlanRepository repository,
        ITripTimeZoneValidator timeZoneValidator,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeZoneValidator = timeZoneValidator ?? throw new ArgumentNullException(nameof(timeZoneValidator));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.activityRecorder = activityRecorder;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<TripPlanResult>>> ListAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            IReadOnlyCollection<TripPlan> trips = await this.repository.ListAccessibleAsync(
                normalizedUserId,
                cancellationToken);
            return ApplicationResult<IReadOnlyCollection<TripPlanResult>>.Success(
                trips.Select(trip => TripPlanResultFactory.ToResult(trip, normalizedUserId)).ToArray());
        }
        catch (ArgumentException exception)
        {
            return Invalid<IReadOnlyCollection<TripPlanResult>>(
                TripPlanErrorCodes.InvalidState,
                exception.Message);
        }
    }

    public async Task<ApplicationResult<TripPlanResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(userId, tripPlanId, out string normalizedUserId, out TripPlanId parsedId))
        {
            return Invalid<TripPlanResult>(TripPlanErrorCodes.InvalidState, "A valid trip identifier is required.");
        }

        TripPlan? trip = await this.repository.GetAccessibleAsync(normalizedUserId, parsedId, cancellationToken);
        return trip is null
            ? ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound())
            : ApplicationResult<TripPlanResult>.Success(
                TripPlanResultFactory.ToResult(trip, normalizedUserId));
    }

    public async Task<ApplicationResult<CreateTripPlanResult>> CreateAsync(
        string userId,
        string clientOperationId,
        TripPlanDetailsInput input,
        CancellationToken cancellationToken)
    {
        try
        {
            string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            string normalizedOperationId = NormalizeOperationId(clientOperationId);
            TripPlan requested = TripPlan.Create(
                TripPlanId.New(),
                normalizedUserId,
                input.Title,
                input.DateProposal,
                input.DestinationTimeZoneId,
                this.timeProvider.GetUtcNow().UtcDateTime);
            IdempotentTripPlanCreationResult? existing = await this.repository.ResolveExistingCreationAsync(
                requested,
                normalizedOperationId,
                cancellationToken);
            if (existing is not null)
            {
                await this.RecordCreationAsync(existing, normalizedUserId, normalizedOperationId);
                return MapCreation(existing, normalizedUserId);
            }

            ApplicationError? timeZoneError = this.ValidateTimeZone(input);
            if (timeZoneError is not null)
            {
                return ApplicationResult<CreateTripPlanResult>.Failure(timeZoneError);
            }

            TripActivityWrite? creationActivity = this.activityRecorder?.CreateWrite(
                requested,
                normalizedUserId,
                TripActivityKind.TripCreated,
                TripActivityRecorder.IdempotentOperationKey(
                    TripActivityKind.TripCreated,
                    normalizedOperationId),
                1);
            IdempotentTripPlanCreationResult outcome = await this.repository.CreateIdempotentAsync(
                requested,
                normalizedOperationId,
                creationActivity,
                cancellationToken);
            await this.RecordCreationAsync(outcome, normalizedUserId, normalizedOperationId);

            return MapCreation(outcome, normalizedUserId);
        }
        catch (TripPlanValidationException exception)
        {
            return Invalid<CreateTripPlanResult>(exception.Code, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Invalid<CreateTripPlanResult>(TripPlanErrorCodes.InvalidState, exception.Message);
        }
    }

    private async Task RecordCreationAsync(
        IdempotentTripPlanCreationResult creation,
        string actorUserId,
        string operationId)
    {
        if (creation.Status is not (IdempotentTripPlanCreationStatus.Created
            or IdempotentTripPlanCreationStatus.Replayed)
            || creation.TripPlan is null
            || this.activityRecorder is null)
        {
            return;
        }

        await this.activityRecorder.RecordAsync(
            creation.TripPlan,
            actorUserId,
            TripActivityKind.TripCreated,
            TripActivityRecorder.IdempotentOperationKey(
                TripActivityKind.TripCreated,
                operationId),
            1,
            CancellationToken.None);
    }

    public Task<ApplicationResult<TripPlanResult>> RenameAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        string title,
        CancellationToken cancellationToken)
    {
        return this.MutateAsync(
            userId,
            tripPlanId,
            expectedVersion,
            TripActivityKind.TripRenamed,
            (trip, nowUtc) => trip.Rename(title, nowUtc),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripPlanResult>> MutateAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        TripActivityKind activityKind,
        Action<TripPlan, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, tripPlanId, expectedVersion, out string normalizedUserId, out TripPlanId parsedId))
        {
            return Invalid<TripPlanResult>(TripPlanErrorCodes.InvalidVersion, "A valid trip and version are required.");
        }

        TripPlan? trip = await this.repository.GetAccessibleAsync(normalizedUserId, parsedId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        TripEffectiveRole? role = trip.ResolveRole(normalizedUserId);
        if (!role.HasValue || !TripAuthorizationPolicy.HasPermission(role.Value, TripPermission.EditPlan))
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (trip.Version != expectedVersion)
        {
            if (expectedVersion < long.MaxValue && trip.Version == expectedVersion + 1)
            {
                long replayedVersion = trip.Version;
                try
                {
                    mutation(trip, nowUtc);
                }
                catch (TripPlanValidationException exception)
                {
                    return Invalid<TripPlanResult>(exception.Code, exception.Message);
                }

                if (trip.Version == replayedVersion)
                {
                    await this.RecordRootActivityAsync(
                        trip,
                        normalizedUserId,
                        activityKind);
                    return ApplicationResult<TripPlanResult>.Success(
                        TripPlanResultFactory.ToResult(trip, normalizedUserId));
                }
            }

            return ApplicationResult<TripPlanResult>.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        try
        {
            mutation(trip, nowUtc);
        }
        catch (TripPlanValidationException exception)
        {
            return Invalid<TripPlanResult>(exception.Code, exception.Message);
        }

        TripPlan persistedTrip = trip;
        if (trip.Version != expectedVersion)
        {
            TripActivityWrite? pendingActivity = this.activityRecorder?.CreateWrite(
                trip,
                normalizedUserId,
                activityKind,
                TripActivityRecorder.RootOperationKey(activityKind, trip.Version),
                1);
            TripPlanWriteResult writeResult = await this.repository.ReplaceAccessibleAsync(
                normalizedUserId,
                trip,
                expectedVersion,
                pendingActivity,
                cancellationToken);
            if (writeResult.Outcome != TripPlanWriteOutcome.Success)
            {
                return writeResult.Outcome == TripPlanWriteOutcome.NotFound
                    ? ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound())
                    : ApplicationResult<TripPlanResult>.Failure(
                        TripPlanApplicationErrors.ChangedConcurrently(writeResult.CurrentVersion));
            }

            persistedTrip = writeResult.PersistedTripPlan
                ?? throw new InvalidOperationException(
                    "A successful trip mutation must return the persisted aggregate.");
        }

        if (this.activityRecorder is not null && persistedTrip.Version != expectedVersion)
        {
            await this.RecordRootActivityAsync(persistedTrip, normalizedUserId, activityKind);
        }

        return ApplicationResult<TripPlanResult>.Success(
            TripPlanResultFactory.ToResult(persistedTrip, normalizedUserId));
    }

    private Task RecordRootActivityAsync(
        TripPlan trip,
        string actorUserId,
        TripActivityKind activityKind)
    {
        return this.activityRecorder?.RecordAsync(
                trip,
                actorUserId,
                activityKind,
                TripActivityRecorder.RootOperationKey(activityKind, trip.Version),
                1,
                CancellationToken.None)
            ?? Task.CompletedTask;
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        DateTime? authenticationConfirmedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, tripPlanId, expectedVersion, out string normalizedUserId, out TripPlanId parsedId))
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.Invalid(
                TripPlanErrorCodes.InvalidVersion,
                "A valid trip and version are required."));
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        if (!IsRecentAuthentication(authenticationConfirmedAtUtc, nowUtc))
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.RecentAuthenticationRequired());
        }

        TripPlan? trip = await this.repository.GetOwnedAsync(normalizedUserId, parsedId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedVersion)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.ChangedConcurrently(trip.Version));
        }

        try
        {
            trip.BeginDeletion(nowUtc);
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.Invalid(
                exception.Code,
                exception.Message));
        }

        TripPlanWriteResult writeResult = await this.repository.DeleteOwnedAsync(
            trip,
            expectedVersion,
            cancellationToken);
        if (writeResult.Outcome == TripPlanWriteOutcome.Success)
        {
            await this.repository.PurgeChildrenAsync(trip.Id, CancellationToken.None);
            writeResult = await this.repository.FinalizeDeletionOwnedAsync(trip, CancellationToken.None);
        }

        return writeResult.Outcome switch
        {
            TripPlanWriteOutcome.Success => ApplicationResult.Success(),
            TripPlanWriteOutcome.NotFound => ApplicationResult.Failure(TripPlanApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(
                TripPlanApplicationErrors.ChangedConcurrently(writeResult.CurrentVersion)),
        };
    }

    private ApplicationError? ValidateTimeZone(TripPlanDetailsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return this.ValidateTimeZone(input.DateProposal, input.DestinationTimeZoneId);
    }

    private ApplicationError? ValidateTimeZone(
        TripDateProposal dateProposal,
        string? destinationTimeZoneId)
    {
        ArgumentNullException.ThrowIfNull(dateProposal);
        if (string.IsNullOrWhiteSpace(destinationTimeZoneId))
        {
            return null;
        }

        return this.timeZoneValidator.IsValidIanaTimeZone(destinationTimeZoneId.Trim())
            ? null
            : TripPlanApplicationErrors.Invalid(
                TripPlanErrorCodes.InvalidTimeZone,
                "The destination time zone must be a valid IANA identifier.");
    }

    private static ApplicationResult<CreateTripPlanResult> MapCreation(
        IdempotentTripPlanCreationResult creation,
        string userId)
    {
        if (creation.Status == IdempotentTripPlanCreationStatus.Conflict)
        {
            return ApplicationResult<CreateTripPlanResult>.Failure(TripPlanApplicationErrors.IdempotencyConflict());
        }

        if (creation.Status == IdempotentTripPlanCreationStatus.LimitReached)
        {
            return ApplicationResult<CreateTripPlanResult>.Failure(TripPlanApplicationErrors.LimitReached());
        }

        if (creation.Status == IdempotentTripPlanCreationStatus.Deleted)
        {
            return ApplicationResult<CreateTripPlanResult>.Failure(
                TripPlanApplicationErrors.CreationWasDeleted());
        }

        TripPlan trip = creation.TripPlan
            ?? throw new InvalidOperationException("A successful trip creation must return its snapshot.");
        return ApplicationResult<CreateTripPlanResult>.Success(new CreateTripPlanResult(
            TripPlanResultFactory.ToResult(trip, userId),
            creation.Status == IdempotentTripPlanCreationStatus.Replayed));
    }

    private static string NormalizeOperationId(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > MaximumIdempotencyKeyLength)
        {
            throw new ArgumentException(
                $"An idempotency key of at most {MaximumIdempotencyKeyLength} characters is required.",
                nameof(value));
        }

        return normalized;
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

    private static bool TryNormalizeMutation(
        string userId,
        string tripPlanId,
        long expectedVersion,
        out string normalizedUserId,
        out TripPlanId parsedId)
    {
        return TryNormalizeIdentity(userId, tripPlanId, out normalizedUserId, out parsedId)
            && expectedVersion > 0;
    }

    private static bool IsRecentAuthentication(DateTime? authenticationConfirmedAtUtc, DateTime nowUtc)
    {
        return authenticationConfirmedAtUtc.HasValue
            && authenticationConfirmedAtUtc.Value.Kind == DateTimeKind.Utc
            && authenticationConfirmedAtUtc.Value >= nowUtc.Subtract(MaximumRecentAuthenticationAge)
            && authenticationConfirmedAtUtc.Value <= nowUtc.Add(AuthenticationClockSkew);
    }

    private static ApplicationResult<TResult> Invalid<TResult>(string code, string message)
    {
        return ApplicationResult<TResult>.Failure(TripPlanApplicationErrors.Invalid(code, message));
    }
}
