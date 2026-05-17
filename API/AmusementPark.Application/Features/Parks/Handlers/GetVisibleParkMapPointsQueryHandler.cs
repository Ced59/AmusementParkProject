using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération des points de carte visibles publiquement.
/// </summary>
public sealed class GetVisibleParkMapPointsQueryHandler : IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>>
{
    private readonly IParkRepository parkRepository;

    public GetVisibleParkMapPointsQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Park>>> HandleAsync(GetVisibleParkMapPointsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetVisibleMapPointsAsync(query.SearchTerm, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Park>>.Success(parks);
    }
}
