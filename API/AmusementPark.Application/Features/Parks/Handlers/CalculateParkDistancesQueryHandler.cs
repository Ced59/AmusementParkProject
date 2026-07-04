using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de calcul de distance entre un parc source et des parcs cibles explicites.
/// </summary>
public sealed class CalculateParkDistancesQueryHandler : IQueryHandler<CalculateParkDistancesQuery, ApplicationResult<ParkDistanceResult>>
{
    private const string DistanceUnit = "km";
    private const string CalculationKind = "great-circle";

    private readonly IParkRepository parkRepository;

    public CalculateParkDistancesQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkDistanceResult>> HandleAsync(CalculateParkDistancesQuery query, CancellationToken cancellationToken = default)
    {
        string sourceParkId = NormalizeParkId(query.SourceParkId);
        if (string.IsNullOrWhiteSpace(sourceParkId))
        {
            return ApplicationResult<ParkDistanceResult>.Failure(ParkApplicationErrors.InvalidDistanceRequest("sourceParkId", "Source park id is required"));
        }

        List<string> targetParkIds = NormalizeParkIds(query.TargetParkIds).ToList();
        if (targetParkIds.Count == 0)
        {
            return ApplicationResult<ParkDistanceResult>.Failure(ParkApplicationErrors.InvalidDistanceRequest("targetParkIds", "At least one target park id is required"));
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

        IReadOnlyCollection<Park> targetParks = await this.parkRepository.GetByIdsAsync(targetParkIds, cancellationToken);
        Dictionary<string, Park> targetParksById = targetParks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .Where(park => query.IncludeHidden || park.IsVisible)
            .GroupBy(static park => park.Id!, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        List<string> missingTargetParkIds = targetParkIds
            .Where(parkId => !targetParksById.ContainsKey(parkId))
            .ToList();

        List<string> unavailableTargetParkIds = targetParksById.Values
            .Where(static park => park.Position is null)
            .Select(static park => park.Id ?? string.Empty)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .ToList();

        List<ParkDistanceTargetResult> targets = targetParksById.Values
            .Where(static park => park.Position is not null)
            .Select(park => this.BuildTarget(sourcePark, park, 0))
            .OrderBy(static target => target.DistanceKilometers)
            .ThenBy(static target => target.Park.Name)
            .ThenBy(static target => target.Park.Id)
            .Select((target, index) => target with { ProximityRank = index + 1 })
            .ToList();

        ParkDistanceResult result = new ParkDistanceResult(
            sourcePark,
            targets,
            missingTargetParkIds,
            unavailableTargetParkIds,
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

    private static string NormalizeParkId(string? parkId)
    {
        return parkId?.Trim() ?? string.Empty;
    }

    private static IReadOnlyCollection<string> NormalizeParkIds(IEnumerable<string>? parkIds)
    {
        if (parkIds is null)
        {
            return Array.Empty<string>();
        }

        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
