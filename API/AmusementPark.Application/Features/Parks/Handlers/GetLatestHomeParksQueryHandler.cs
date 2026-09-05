using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

public sealed class GetLatestHomeParksQueryHandler
    : IQueryHandler<GetLatestHomeParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>>
{
    private const int DefaultLimit = 3;
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 3;

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetLatestHomeParksQueryHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> HandleAsync(
        GetLatestHomeParksQuery query,
        CancellationToken cancellationToken = default)
    {
        int requestedLimit = query.Limit <= 0 ? DefaultLimit : query.Limit;
        int normalizedLimit = Math.Clamp(requestedLimit, MinimumLimit, MaximumLimit);
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetLatestVisibleAsync(
            normalizedLimit,
            ClosedEntityFilter.OpenOnly,
            cancellationToken);
        IReadOnlyCollection<HomeFeaturedParkResult> results = await HomeParkCardResultBuilder.BuildAsync(
            parks,
            this.parkItemRepository,
            cancellationToken);

        return ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>.Success(results);
    }
}
