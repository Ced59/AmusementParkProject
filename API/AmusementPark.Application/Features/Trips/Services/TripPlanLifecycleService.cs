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

    private readonly ITripPlanRepository repository;
    private readonly ITripTimeZoneValidator timeZoneValidator;
    private readonly TimeProvider timeProvider;

    public TripPlanLifecycleService(
        ITripPlanRepository repository,
        ITripTimeZoneValidator timeZoneValidator,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeZoneValidator = timeZoneValidator ?? throw new ArgumentNullException(nameof(timeZoneValidator));
        this.timeProvider = timeProvider ?? TimeProvider.System;
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
                trips.Select(trip => ToResult(trip, normalizedUserId)).ToArray());
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
            : ApplicationResult<TripPlanResult>.Success(ToResult(trip, normalizedUserId));
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
            ApplicationError? timeZoneError = this.ValidateTimeZone(input);
            if (timeZoneError is not null)
            {
                return ApplicationResult<CreateTripPlanResult>.Failure(timeZoneError);
            }

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
            IdempotentTripPlanCreationResult outcome = existing
                ?? await this.repository.CreateIdempotentAsync(requested, normalizedOperationId, cancellationToken);
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
            (trip, nowUtc) => trip.Rename(title, nowUtc),
            cancellationToken);
    }

    public Task<ApplicationResult<TripPlanResult>> SetDatesAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        TripPlanDatesInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        ApplicationError? timeZoneError = this.ValidateTimeZone(
            input.DateProposal,
            input.DestinationTimeZoneId);
        if (timeZoneError is not null)
        {
            return Task.FromResult(ApplicationResult<TripPlanResult>.Failure(timeZoneError));
        }

        return this.MutateAsync(
            userId,
            tripPlanId,
            expectedVersion,
            (trip, nowUtc) => trip.SetDates(
                input.DateProposal,
                input.DestinationTimeZoneId,
                nowUtc),
            cancellationToken);
    }

    private async Task<ApplicationResult<TripPlanResult>> MutateAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        Action<TripPlan, DateTime> mutation,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, tripPlanId, expectedVersion, out string normalizedUserId, out TripPlanId parsedId))
        {
            return Invalid<TripPlanResult>(TripPlanErrorCodes.InvalidVersion, "A valid trip and version are required.");
        }

        TripPlan? trip = await this.repository.GetOwnedAsync(normalizedUserId, parsedId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedVersion)
        {
            return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.ChangedConcurrently());
        }

        try
        {
            mutation(trip, this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return Invalid<TripPlanResult>(exception.Code, exception.Message);
        }

        if (trip.Version != expectedVersion)
        {
            TripPlanWriteOutcome outcome = await this.repository.ReplaceOwnedAsync(
                trip,
                expectedVersion,
                cancellationToken);
            if (outcome != TripPlanWriteOutcome.Success)
            {
                return ApplicationResult<TripPlanResult>.Failure(TripPlanApplicationErrors.ChangedConcurrently());
            }
        }

        return ApplicationResult<TripPlanResult>.Success(ToResult(trip, normalizedUserId));
    }

    public async Task<ApplicationResult> DeleteAsync(
        string userId,
        string tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMutation(userId, tripPlanId, expectedVersion, out string normalizedUserId, out TripPlanId parsedId))
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.Invalid(
                TripPlanErrorCodes.InvalidVersion,
                "A valid trip and version are required."));
        }

        TripPlan? trip = await this.repository.GetOwnedAsync(normalizedUserId, parsedId, cancellationToken);
        if (trip is null)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.NotFound());
        }

        if (trip.Version != expectedVersion)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.ChangedConcurrently());
        }

        try
        {
            trip.BeginDeletion(this.timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (TripPlanValidationException exception)
        {
            return ApplicationResult.Failure(TripPlanApplicationErrors.Invalid(
                exception.Code,
                exception.Message));
        }

        TripPlanWriteOutcome outcome = await this.repository.DeleteOwnedAsync(
            trip,
            expectedVersion,
            cancellationToken);
        return outcome switch
        {
            TripPlanWriteOutcome.Success => ApplicationResult.Success(),
            TripPlanWriteOutcome.NotFound => ApplicationResult.Failure(TripPlanApplicationErrors.NotFound()),
            _ => ApplicationResult.Failure(TripPlanApplicationErrors.ChangedConcurrently()),
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
            ToResult(trip, userId),
            creation.Status == IdempotentTripPlanCreationStatus.Replayed));
    }

    private static TripPlanResult ToResult(TripPlan trip, string currentUserId)
    {
        return new TripPlanResult(
            trip.Id.Value,
            trip.Title,
            new TripDateProposalResult(
                trip.DateProposal.Kind,
                trip.DateProposal.StartDate,
                trip.DateProposal.EndDate,
                trip.DateProposal.CandidateDates),
            trip.DestinationTimeZoneId,
            trip.Status,
            trip.AccessScope,
            trip.Members.Count(member => member.State == TripMembershipState.Active),
            string.Equals(trip.OwnerUserId, currentUserId, StringComparison.Ordinal),
            trip.CreatedAtUtc,
            trip.UpdatedAtUtc,
            trip.Version);
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

    private static ApplicationResult<TResult> Invalid<TResult>(string code, string message)
    {
        return ApplicationResult<TResult>.Failure(TripPlanApplicationErrors.Invalid(code, message));
    }
}
