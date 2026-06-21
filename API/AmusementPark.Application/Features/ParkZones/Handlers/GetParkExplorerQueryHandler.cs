using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkExplorerQueryHandler : IQueryHandler<GetParkExplorerQuery, ApplicationResult<ParkExplorerResult>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;

    public GetParkExplorerQueryHandler(IParkZoneRepository parkZoneRepository, IParkRepository parkRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkExplorerResult>> HandleAsync(GetParkExplorerQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkExplorerResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkExplorerResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        ParkExplorerResult explorer = await this.parkZoneRepository.GetExplorerAsync(query.ParkId.Trim(), query.IncludeHidden, query.ClosedFilter, cancellationToken);
        return ApplicationResult<ParkExplorerResult>.Success(explorer);
    }
}
