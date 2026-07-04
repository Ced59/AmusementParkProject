using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération des parcs les plus proches d'un parc source.
/// </summary>
public sealed class GetNearestParksQueryHandler : IQueryHandler<GetNearestParksQuery, ApplicationResult<ParkDistanceResult>>
{
    private const string DistanceUnit = "km";
    private const string CalculationKind = "great-circle";
    private const int DefaultLimit = 4;
    private const int MaximumLimit = 50;

    private readonly IParkRepository parkRepository;

    public GetNearestParksQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkDistanceResult>> HandleAsync(GetNearestParksQuery query, CancellationToken cancellationToken = default)
    {
        string sourceParkId = query.SourceParkId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sourceParkId))
        {
            return ApplicationResult<ParkDistanceResult>.Failure(ParkApplicationErrors.InvalidDistanceRequest("sourceParkId", "Source park id is required"));
        }

        Park? sourcePark = await this.parkRepository.GetByIdAsync(sourceParkId, query.IncludeHidden, cancellationToken);
        if (sourcePark is null)
        {
            return ApplicationResult<ParkDistanceResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        if (sourcePark.Position is null)
        {
            return ApplicationResult<ParkDistanceResult>.Failure(ParkApplicationErrors.ParkHasNoCoordinates(sourceParkId));
        }

        int limit = NormalizeLimit(query.Limit);
        int queryLimit = limit + 1;
        IReadOnlyCollection<Park> nearbyCandidates = await this.parkRepository.GetNearestByLocationAsync(
            sourcePark.Position.Latitude,
            sourcePark.Position.Longitude,
            queryLimit,
            query.MaxDistanceKilometers,
            includeHidden: false,
            closedFilter: query.ClosedFilter,
            cancellationToken: cancellationToken);

        List<ParkDistanceTargetResult> targets = nearbyCandidates
            .Where(candidate => !string.Equals(candidate.Id, sourcePark.Id, StringComparison.Ordinal))
            .Where(static candidate => candidate.IsVisible)
            .Where(static candidate => candidate.Position is not null)
            .Select(candidate => this.BuildTarget(sourcePark, candidate, 0))
            .OrderBy(static target => target.DistanceKilometers)
            .ThenBy(static target => target.Park.Name)
            .ThenBy(static target => target.Park.Id)
            .Take(limit)
            .Select((target, index) => target with { ProximityRank = index + 1 })
            .ToList();

        ParkDistanceResult result = new ParkDistanceResult(
            sourcePark,
            targets,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DistanceUnit,
            CalculationKind);

        return ApplicationResult<ParkDistanceResult>.Success(result);
    }

    private ParkDistanceTargetResult BuildTarget(Park sourcePark, Park targetPark, int proximityRank)
    {
        double distanceKilometers = Math.Round(GeoDistanceCalculator.CalculateKilometers(sourcePark.Position!, targetPark.Position!), 2, MidpointRounding.AwayFromZero);
        int estimatedTravelDurationMinutes = GeoDistanceCalculator.EstimateTravelDurationMinutes(distanceKilometers);
        return new ParkDistanceTargetResult(targetPark, distanceKilometers, estimatedTravelDurationMinutes, proximityRank);
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaximumLimit);
    }
}
