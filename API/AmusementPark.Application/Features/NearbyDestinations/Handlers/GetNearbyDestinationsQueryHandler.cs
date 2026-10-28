using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.NearbyDestinations.Queries;
using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.NearbyDestinations.Handlers;

public sealed class GetNearbyDestinationsQueryHandler
    : IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>>
{
    private readonly IParkRepository parks;
    private readonly IStandaloneAttractionRepository attractions;
    private readonly IImageRepository images;

    public GetNearbyDestinationsQueryHandler(IParkRepository parks, IStandaloneAttractionRepository attractions, IImageRepository images)
    {
        this.parks = parks;
        this.attractions = attractions;
        this.images = images;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>> HandleAsync(
        GetNearbyDestinationsQuery query, CancellationToken cancellationToken = default)
    {
        string sourceId = query.SourceId?.Trim() ?? string.Empty;
        if (sourceId.Length == 0 || !Enum.IsDefined(query.SourceKind))
        {
            return ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Failure(
                ApplicationError.Validation("nearby.invalid-source", "A valid source kind and id are required."));
        }

        Park? sourcePark = query.SourceKind == NearbyDestinationKind.Park
            ? await this.parks.GetByIdAsync(sourceId, false, cancellationToken)
            : null;
        StandaloneAttraction? sourceAttraction = query.SourceKind == NearbyDestinationKind.StandaloneAttraction
            ? await this.attractions.GetByIdAsync(sourceId, false, cancellationToken)
            : null;
        if (sourcePark?.IsVisible != true && sourceAttraction?.IsPubliclyPublishable() != true)
        {
            return ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Failure(
                ApplicationError.NotFound("nearby.source-not-found", "Public destination not found."));
        }

        GeoPoint? position = sourcePark?.Position ?? sourceAttraction?.Position;
        if (position is null || !HasValidPosition(position))
        {
            return ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Success(Array.Empty<NearbyDestinationResult>());
        }

        int limit = query.Limit <= 0 ? 4 : Math.Min(query.Limit, 50);
        // Each indexed query is bounded. Fetching one extra candidate allows excluding the source.
        IReadOnlyCollection<Park> parkCandidates = await this.parks.GetNearestByLocationAsync(
            position.Latitude, position.Longitude, limit + 1, null, false, ClosedEntityFilter.OpenOnly, cancellationToken);
        IReadOnlyCollection<StandaloneAttraction> attractionCandidates = await this.attractions.GetNearestByLocationAsync(
            position.Latitude, position.Longitude, limit + 1, cancellationToken);

        IEnumerable<NearbyDestinationResult> nearbyParks = parkCandidates
            .Where(candidate => candidate.IsVisible && HasValidPosition(candidate.Position))
            .Where(candidate => query.SourceKind != NearbyDestinationKind.Park || candidate.Id != sourceId)
            .Select(candidate => new NearbyDestinationResult(candidate, null,
                GeoDistanceCalculator.CalculateKilometers(position, candidate.Position!), 0, 0));
        IEnumerable<NearbyDestinationResult> nearbyAttractions = attractionCandidates
            .Where(candidate => candidate.IsPubliclyPublishable() && HasValidPosition(candidate.Position))
            .Where(candidate => query.SourceKind != NearbyDestinationKind.StandaloneAttraction || candidate.Id != sourceId)
            .Select(candidate => new NearbyDestinationResult(null, candidate,
                GeoDistanceCalculator.CalculateKilometers(position, candidate.Position!), 0, 0));

        List<NearbyDestinationResult> targets = nearbyParks.Concat(nearbyAttractions)
            .OrderBy(static target => target.DistanceKilometers)
            .ThenBy(static target => target.Park?.Name ?? target.StandaloneAttraction!.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static target => target.Park?.Id ?? target.StandaloneAttraction!.Id, StringComparer.Ordinal)
            .Take(limit)
            .Select((target, index) => target with
            {
                DistanceKilometers = Math.Round(target.DistanceKilometers, 2, MidpointRounding.AwayFromZero),
                EstimatedTravelDurationMinutes = GeoDistanceCalculator.EstimateTravelDurationMinutes(target.DistanceKilometers),
                ProximityRank = index + 1
            })
            .ToList();

        List<string> attractionIds = targets
            .Where(static target => target.StandaloneAttraction is not null)
            .Select(static target => target.StandaloneAttraction!.Id)
            .ToList();
        if (attractionIds.Count > 0)
        {
            IReadOnlyDictionary<string, string> photoIds = await this.images.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.StandaloneAttraction, attractionIds, ImageCategory.StandaloneAttraction,
                publishedOnly: true, cancellationToken);
            targets = targets.Select(target => target with
            {
                PhotoImageId = target.StandaloneAttraction is not null &&
                    photoIds.TryGetValue(target.StandaloneAttraction.Id, out string? photoId) ? photoId : null
            }).ToList();
        }

        return ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>.Success(targets);
    }

    private static bool HasValidPosition(GeoPoint? position)
    {
        return position is not null && double.IsFinite(position.Latitude) && double.IsFinite(position.Longitude);
    }
}
