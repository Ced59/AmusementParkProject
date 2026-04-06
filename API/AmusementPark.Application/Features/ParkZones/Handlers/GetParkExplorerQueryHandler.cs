using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Application.Features.ParkZones.Results;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkExplorerQueryHandler : IQueryHandler<GetParkExplorerQuery, ApplicationResult<ParkExplorerResult>>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public GetParkExplorerQueryHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult<ParkExplorerResult>> HandleAsync(GetParkExplorerQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkExplorerResult>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        ParkExplorerResult explorer = await this.parkZoneRepository.GetExplorerAsync(query.ParkId, query.IncludeHidden, cancellationToken);
        return ApplicationResult<ParkExplorerResult>.Success(explorer);
    }
}
