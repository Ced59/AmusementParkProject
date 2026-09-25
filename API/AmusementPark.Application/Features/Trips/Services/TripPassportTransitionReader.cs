using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPassportTransitionReader
{
    private readonly ITripPlanRepository trips;
    private readonly TripProgramResultFactory programFactory;
    private readonly ITripPreferenceRepository preferences;
    private readonly IParkItemRepository parkItems;
    private readonly IImageRepository images;
    private readonly IUserVisitRepository visits;
    private readonly IPassportLocalDateResolver localDateResolver;
    private readonly TimeProvider timeProvider;

    public TripPassportTransitionReader(
        ITripPlanRepository trips,
        TripProgramResultFactory programFactory,
        ITripPreferenceRepository preferences,
        IParkItemRepository parkItems,
        IImageRepository images,
        IUserVisitRepository visits,
        IPassportLocalDateResolver localDateResolver,
        TimeProvider? timeProvider = null)
    {
        this.trips = trips ?? throw new ArgumentNullException(nameof(trips));
        this.programFactory = programFactory ?? throw new ArgumentNullException(nameof(programFactory));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.parkItems = parkItems ?? throw new ArgumentNullException(nameof(parkItems));
        this.images = images ?? throw new ArgumentNullException(nameof(images));
        this.visits = visits ?? throw new ArgumentNullException(nameof(visits));
        this.localDateResolver = localDateResolver
            ?? throw new ArgumentNullException(nameof(localDateResolver));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripPassportTransitionResult>> GetAsync(
        string userId,
        string tripPlanId,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeIdentity(
            userId,
            tripPlanId,
            out string normalizedUserId,
            out TripPlanId parsedTripId))
        {
            return Failure(TripPlanApplicationErrors.NotFound());
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
            return ApplicationResult<TripPassportTransitionResult>.Failure(programResult.Errors);
        }

        if (!TryResolveDestinationToday(trip, out DateOnly destinationToday))
        {
            return Failure(TripPlanApplicationErrors.PassportTransitionNotReady());
        }

        TripDayPlanResult[] days = programResult.Value.Program.Days
            .OrderBy(static day => day.LocalDate)
            .ToArray();
        IReadOnlyCollection<Visit> existingVisits = await this.visits.ListOwnedByExactDatesAsync(
            normalizedUserId,
            days.Select(static day => day.LocalDate).ToArray(),
            cancellationToken);
        Dictionary<(string ParkId, DateOnly Date), Visit> existingByDay = existingVisits
            .Where(static visit => visit.Date.Precision == VisitDatePrecision.Day
                && visit.Date.Month.HasValue
                && visit.Date.Day.HasValue)
            .GroupBy(static visit => (
                visit.ParkId,
                new DateOnly(visit.Date.Year, visit.Date.Month!.Value, visit.Date.Day!.Value)))
            .ToDictionary(
                static group => group.Key,
                static group => group.OrderByDescending(visit => visit.UpdatedAtUtc).First());

        string[] pastParkIds = days
            .Where(day => TripPassportTransitionPolicy.CanConfirmDay(
                day.LocalDate,
                destinationToday,
                day.IsParkAvailable))
            .Select(static day => day.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<ParkItem> availableItems = pastParkIds.Length == 0
            ? Array.Empty<ParkItem>()
            : await this.parkItems.GetByParkIdsAsync(
                pastParkIds,
                includeHidden: false,
                cancellationToken);
        ParkItem[] attractions = availableItems
            .Where(static item => item.Category == ParkItemCategory.Attraction
                && item.IsVisible
                && !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.Name))
            .ToArray();
        IReadOnlyCollection<TripItemPreference> ownPreferences =
            await this.preferences.ListForUserAsync(
                parsedTripId,
                normalizedUserId,
                cancellationToken);
        Dictionary<string, TripItemPreferenceLevel> preferenceByItem = ownPreferences
            .GroupBy(static preference => preference.ParkItemId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.OrderByDescending(preference => preference.UpdatedAtUtc).First().Level,
                StringComparer.Ordinal);
        IReadOnlyDictionary<string, string> imageIds = attractions.Length == 0
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : await this.images.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                attractions.Select(static item => item.Id!).ToArray(),
                ImageCategory.ParkItem,
                true,
                cancellationToken);

        TripPassportTransitionDayResult[] results = days.Select(day =>
        {
            existingByDay.TryGetValue((day.ParkId, day.LocalDate), out Visit? existing);
            bool canConfirm = TripPassportTransitionPolicy.CanConfirmDay(
                    day.LocalDate,
                    destinationToday,
                    day.IsParkAvailable)
                && existing is null;
            IReadOnlyCollection<TripPassportTransitionItemResult> dayItems = canConfirm
                ? BuildItems(
                    attractions.Where(item => string.Equals(
                        item.ParkId,
                        day.ParkId,
                        StringComparison.Ordinal)),
                    day.LocalDate,
                    preferenceByItem,
                    imageIds)
                : Array.Empty<TripPassportTransitionItemResult>();
            return new TripPassportTransitionDayResult(
                day.LocalDate,
                day.ParkId,
                day.ParkName,
                day.IsParkAvailable,
                canConfirm,
                existing?.Id.Value,
                existing?.Status,
                dayItems);
        }).ToArray();

        return ApplicationResult<TripPassportTransitionResult>.Success(
            new TripPassportTransitionResult(trip.Title, destinationToday, results));
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

    private static IReadOnlyCollection<TripPassportTransitionItemResult> BuildItems(
        IEnumerable<ParkItem> items,
        DateOnly localDate,
        IReadOnlyDictionary<string, TripItemPreferenceLevel> preferenceByItem,
        IReadOnlyDictionary<string, string> imageIds)
    {
        VisitDate visitDate = VisitDate.ForDay(localDate.Year, localDate.Month, localDate.Day);
        return items.OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Id, StringComparer.Ordinal)
            .Select(item => new TripPassportTransitionItemResult(
                item.Id!,
                item.Name.Trim(),
                imageIds.GetValueOrDefault(item.Id!),
                preferenceByItem.GetValueOrDefault(
                    item.Id!,
                    TripItemPreferenceLevel.Unknown),
                RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                    visitDate,
                    ToDateOnly(item.AttractionDetails?.OpeningDate),
                    ToDateOnly(item.AttractionDetails?.ClosingDate))))
            .ToArray();
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
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

    private static ApplicationResult<TripPassportTransitionResult> Failure(
        ApplicationError error)
    {
        return ApplicationResult<TripPassportTransitionResult>.Failure(error);
    }
}
