using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPassportTransitionConfirmer
{
    private const int MaximumSelectedAttractions = 100;
    private readonly ITripPlanRepository trips;
    private readonly TripProgramResultFactory programFactory;
    private readonly IParkItemRepository parkItems;
    private readonly IUserVisitRepository visits;
    private readonly IPassportLocalDateResolver localDateResolver;
    private readonly ICommandHandler<CreateVisitCommand,
        ApplicationResult<CreateVisitResult>> createVisitHandler;
    private readonly ICommandHandler<AddRideOccurrencesBatchCommand,
        ApplicationResult<CreateRideOccurrencesResult>> addRidesHandler;
    private readonly TimeProvider timeProvider;

    public TripPassportTransitionConfirmer(
        ITripPlanRepository trips,
        TripProgramResultFactory programFactory,
        IParkItemRepository parkItems,
        IUserVisitRepository visits,
        IPassportLocalDateResolver localDateResolver,
        ICommandHandler<CreateVisitCommand,
            ApplicationResult<CreateVisitResult>> createVisitHandler,
        ICommandHandler<AddRideOccurrencesBatchCommand,
            ApplicationResult<CreateRideOccurrencesResult>> addRidesHandler,
        TimeProvider? timeProvider = null)
    {
        this.trips = trips ?? throw new ArgumentNullException(nameof(trips));
        this.programFactory = programFactory ?? throw new ArgumentNullException(nameof(programFactory));
        this.parkItems = parkItems ?? throw new ArgumentNullException(nameof(parkItems));
        this.visits = visits ?? throw new ArgumentNullException(nameof(visits));
        this.localDateResolver = localDateResolver
            ?? throw new ArgumentNullException(nameof(localDateResolver));
        this.createVisitHandler = createVisitHandler
            ?? throw new ArgumentNullException(nameof(createVisitHandler));
        this.addRidesHandler = addRidesHandler
            ?? throw new ArgumentNullException(nameof(addRidesHandler));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ConfirmTripPassportTransitionResult>> ConfirmAsync(
        string userId,
        string tripPlanId,
        DateOnly localDate,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(
            userId,
            tripPlanId,
            out string normalizedUserId,
            out TripPlanId parsedTripId)
            || !TryNormalizeSelection(parkItemIds, out string[] normalizedItemIds))
        {
            return Failure(TripPlanApplicationErrors.PassportTransitionInvalidSelection());
        }

        TripPlan? trip = await this.trips.GetAccessibleAsync(
            normalizedUserId,
            parsedTripId,
            cancellationToken);
        if (trip is null || trip.ResolveRole(normalizedUserId) is null)
        {
            return Failure(TripPlanApplicationErrors.NotFound());
        }

        ApplicationResult<TripProgramSnapshotResult> programResult =
            await this.programFactory.BuildSnapshotAsync(parsedTripId, cancellationToken);
        if (!programResult.IsSuccess || programResult.Value is null)
        {
            return ApplicationResult<ConfirmTripPassportTransitionResult>.Failure(programResult.Errors);
        }

        TripDayPlanResult? day = programResult.Value.Program.Days.SingleOrDefault(
            candidate => candidate.LocalDate == localDate);
        if (day is null
            || !day.IsParkAvailable
            || !TryResolveDestinationToday(trip, out DateOnly destinationToday)
            || localDate >= destinationToday)
        {
            return Failure(TripPlanApplicationErrors.PassportTransitionNotReady());
        }

        IReadOnlyCollection<Visit> sameDateVisits = await this.visits.ListOwnedByExactDatesAsync(
            normalizedUserId,
            new[] { localDate },
            cancellationToken);
        Visit? existingVisit = sameDateVisits
            .Where(visit => string.Equals(visit.ParkId, day.ParkId, StringComparison.Ordinal)
                && visit.Date.Precision == VisitDatePrecision.Day
                && visit.Date.Month == localDate.Month
                && visit.Date.Day == localDate.Day)
            .OrderByDescending(static visit => visit.UpdatedAtUtc)
            .FirstOrDefault();
        string visitOperationId = TripPassportTransitionOperationKeys.Visit(
            parsedTripId.Value,
            normalizedUserId,
            localDate);
        if (existingVisit is not null
            && !await this.IsTransitionCreationAsync(
                trip,
                day,
                normalizedUserId,
                localDate,
                visitOperationId,
                cancellationToken))
        {
            return ApplicationResult<ConfirmTripPassportTransitionResult>.Success(
                new ConfirmTripPassportTransitionResult(existingVisit.Id.Value, true, 0));
        }

        IReadOnlyCollection<ParkItem> dayItems = await this.parkItems.GetByParkIdAsync(
            day.ParkId,
            includeHidden: false,
            cancellationToken);
        HashSet<string> selectableIds = dayItems
            .Where(static item => item.Category == ParkItemCategory.Attraction
                && item.IsVisible
                && !string.IsNullOrWhiteSpace(item.Id))
            .Select(static item => item.Id!)
            .ToHashSet(StringComparer.Ordinal);
        if (normalizedItemIds.Any(itemId => !selectableIds.Contains(itemId)))
        {
            return Failure(TripPlanApplicationErrors.PassportTransitionInvalidSelection());
        }

        CreateVisitCommand createCommand = new CreateVisitCommand(
            normalizedUserId,
            visitOperationId,
            day.ParkId,
            localDate.Year,
            localDate.Month,
            localDate.Day,
            VisitDatePrecision.Day,
            false,
            trip.DestinationTimeZoneId,
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null);
        ApplicationResult<CreateVisitResult> visitResult =
            await this.createVisitHandler.HandleAsync(createCommand, cancellationToken);
        if (!visitResult.IsSuccess || visitResult.Value is null)
        {
            return ApplicationResult<ConfirmTripPassportTransitionResult>.Failure(
                visitResult.Errors);
        }

        int addedRideCount = 0;
        bool wasReplayed = visitResult.Value.WasReplayed;
        if (normalizedItemIds.Length > 0)
        {
            Visit? currentVisit = await this.visits.GetOwnedAsync(
                VisitId.Parse(visitResult.Value.Visit.Id),
                normalizedUserId,
                cancellationToken);
            if (currentVisit?.Status == VisitStatus.Draft)
            {
                AddRideOccurrencesBatchCommand addRidesCommand =
                    new AddRideOccurrencesBatchCommand(
                        normalizedUserId,
                        visitResult.Value.Visit.Id,
                        TripPassportTransitionOperationKeys.Rides(
                            parsedTripId.Value,
                            normalizedUserId,
                            localDate),
                        normalizedItemIds.Select(static itemId =>
                            (RideOccurrenceCreationItem?)new RideOccurrenceCreationItem(
                                itemId,
                                null,
                                false,
                                RideOccurrenceStatus.Completed,
                                null,
                                true)).ToArray(),
                        RideLogSource.TripTransition);
                ApplicationResult<CreateRideOccurrencesResult> ridesResult =
                    await this.addRidesHandler.HandleAsync(addRidesCommand, cancellationToken);
                if (!ridesResult.IsSuccess || ridesResult.Value is null)
                {
                    return ApplicationResult<ConfirmTripPassportTransitionResult>.Failure(
                        ridesResult.Errors);
                }

                addedRideCount = ridesResult.Value.Occurrences.Count;
                wasReplayed |= ridesResult.Value.WasReplayed;
            }
        }

        return ApplicationResult<ConfirmTripPassportTransitionResult>.Success(
            new ConfirmTripPassportTransitionResult(
                visitResult.Value.Visit.Id,
                wasReplayed,
                addedRideCount));
    }

    private async Task<bool> IsTransitionCreationAsync(
        TripPlan trip,
        TripDayPlanResult day,
        string userId,
        DateOnly localDate,
        string visitOperationId,
        CancellationToken cancellationToken)
    {
        Visit requestedVisit = Visit.Create(
            VisitId.New(),
            userId,
            day.ParkId,
            VisitDate.ForDay(localDate.Year, localDate.Month, localDate.Day),
            trip.DestinationTimeZoneId,
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null,
            this.timeProvider.GetUtcNow().UtcDateTime);
        IdempotentVisitCreationResult? existingTransition =
            await this.visits.ResolveExistingCreationAsync(
                requestedVisit,
                visitOperationId,
                cancellationToken);
        return existingTransition is not null;
    }

    private bool TryResolveDestinationToday(TripPlan trip, out DateOnly destinationToday)
    {
        if (string.IsNullOrWhiteSpace(trip.DestinationTimeZoneId))
        {
            destinationToday = default;
            return false;
        }

        try
        {
            destinationToday = this.localDateResolver.Resolve(
                this.timeProvider.GetUtcNow().UtcDateTime,
                trip.DestinationTimeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            destinationToday = default;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            destinationToday = default;
            return false;
        }
    }

    private static bool TryNormalizeIdentity(
        string userId,
        string tripPlanId,
        out string normalizedUserId,
        out TripPlanId parsedTripId)
    {
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
            return TripPlanId.TryParse(tripPlanId, out parsedTripId);
        }
        catch (ArgumentException)
        {
            normalizedUserId = string.Empty;
            parsedTripId = default;
            return false;
        }
    }

    private static bool TryNormalizeSelection(
        IReadOnlyCollection<string>? parkItemIds,
        out string[] normalizedItemIds)
    {
        normalizedItemIds = Array.Empty<string>();
        if (parkItemIds is null || parkItemIds.Count > MaximumSelectedAttractions)
        {
            return false;
        }

        try
        {
            normalizedItemIds = parkItemIds
                .Select(static itemId => IdentifierRules.NormalizeRequired(itemId, nameof(parkItemIds)))
                .ToArray();
            return normalizedItemIds.Distinct(StringComparer.Ordinal).Count()
                == normalizedItemIds.Length;
        }
        catch (ArgumentException)
        {
            normalizedItemIds = Array.Empty<string>();
            return false;
        }
    }

    private static ApplicationResult<ConfirmTripPassportTransitionResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<ConfirmTripPassportTransitionResult>.Failure(error);
    }
}
