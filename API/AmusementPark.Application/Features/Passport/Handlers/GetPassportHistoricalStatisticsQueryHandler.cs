using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportHistoricalStatisticsQueryHandler
    : IQueryHandler<
        GetPassportHistoricalStatisticsQuery,
        ApplicationResult<PassportHistoricalStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IPassportHistoricalTargetResolver historicalTargetResolver;
    private readonly IParkNameReadRepository parkNameReadRepository;
    private readonly TimeProvider timeProvider;

    public GetPassportHistoricalStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IPassportHistoricalTargetResolver historicalTargetResolver,
        IParkNameReadRepository parkNameReadRepository,
        TimeProvider? timeProvider = null)
    {
        this.sourceReader = sourceReader;
        this.historicalTargetResolver = historicalTargetResolver;
        this.parkNameReadRepository = parkNameReadRepository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PassportHistoricalStatisticsResult>> HandleAsync(
        GetPassportHistoricalStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        string userId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<PassportHistoricalStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        PassportGlobalStatisticsSource source = await this.sourceReader.ReadGlobalAsync(
            userId,
            null,
            null,
            cancellationToken);
        DateOnly today = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
        VisitDate currentDate = VisitDate.ForDay(today.Year, today.Month, today.Day);
        Dictionary<string, PassportHistoricalVisitContextObservation> visitsById =
            new Dictionary<string, PassportHistoricalVisitContextObservation>(StringComparer.Ordinal);
        Dictionary<string, PassportHistoricalTargetContext> currentContextsByPark =
            new Dictionary<string, PassportHistoricalTargetContext>(StringComparer.Ordinal);

        foreach (IGrouping<string, PassportVisitStatisticsObservation> parkVisits in
                 source.Visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal))
        {
            VisitDate[] dates = parkVisits.Select(static visit => visit.VisitDate)
                .Append(currentDate)
                .Distinct()
                .ToArray();
            IReadOnlyDictionary<VisitDate, PassportHistoricalTargetContext> contexts =
                await this.historicalTargetResolver.ResolveAllManyAsync(
                    parkVisits.Key,
                    dates,
                    cancellationToken);
            if (contexts.TryGetValue(currentDate, out PassportHistoricalTargetContext? current))
            {
                currentContextsByPark[parkVisits.Key] = current;
            }

            foreach (PassportVisitStatisticsObservation visit in parkVisits)
            {
                contexts.TryGetValue(visit.VisitDate, out PassportHistoricalTargetContext? context);
                visitsById[visit.VisitId] = CreateVisitObservation(visit, context);
            }
        }

        PassportHistoricalRideContextObservation[] rides = source.Rides
            .Where(ride => visitsById.ContainsKey(ride.VisitId))
            .Select(ride => CreateRideObservation(
                ride,
                visitsById[ride.VisitId],
                currentContextsByPark.GetValueOrDefault(ride.ParkId)))
            .ToArray();
        PassportHistoricalStatistics statistics =
            PassportHistoricalStatisticsCalculator.Calculate(
                visitsById.Values.ToArray(),
                rides);
        string[] parkIds = source.Visits.Select(static visit => visit.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, string?> parkNames = parkIds.Length == 0
            ? new Dictionary<string, string?>(StringComparer.Ordinal)
            : await this.parkNameReadRepository.GetNamesByIdsAsync(
                parkIds,
                cancellationToken);
        return ApplicationResult<PassportHistoricalStatisticsResult>.Success(
            PassportHistoricalStatisticsResultFactory.Create(statistics, parkNames));
    }

    private static PassportHistoricalVisitContextObservation CreateVisitObservation(
        PassportVisitStatisticsObservation visit,
        PassportHistoricalTargetContext? context)
    {
        PassportHistoricalTargetStateObservation[] targets = context?.Targets.Values
            .Select(ToObservation)
            .ToArray()
            ?? Array.Empty<PassportHistoricalTargetStateObservation>();
        return new PassportHistoricalVisitContextObservation(
            visit.VisitId,
            visit.ParkId,
            visit.VisitDate,
            context?.CoverageStatus
                ?? AmusementPark.Core.Domain.History.HistoricalCoverageStatus.Partial,
            targets);
    }

    private static PassportHistoricalRideContextObservation CreateRideObservation(
        PassportRideStatisticsObservation ride,
        PassportHistoricalVisitContextObservation visit,
        PassportHistoricalTargetContext? currentContext)
    {
        PassportHistoricalTargetStateObservation? targetAtVisit = visit.Targets
            .FirstOrDefault(target => string.Equals(
                target.ParkItemId,
                ride.ParkItemId,
                StringComparison.Ordinal));
        PassportHistoricalTargetStateObservation? currentTarget = null;
        if (currentContext?.Targets.TryGetValue(
                ride.ParkItemId,
                out PassportHistoricalTarget? resolvedCurrent) == true)
        {
            currentTarget = ToObservation(resolvedCurrent);
        }

        return new PassportHistoricalRideContextObservation(
            ride.RideOccurrenceId,
            ride.VisitId,
            ride.ParkId,
            ride.ParkItemId,
            ride.VisitDate,
            ride.Status,
            targetAtVisit,
            currentTarget);
    }

    private static PassportHistoricalTargetStateObservation ToObservation(
        PassportHistoricalTarget target)
    {
        return new PassportHistoricalTargetStateObservation(
            target.ParkItemId,
            target.Name,
            target.Category,
            target.OperationalState,
            !target.IsValidationFallback);
    }
}
