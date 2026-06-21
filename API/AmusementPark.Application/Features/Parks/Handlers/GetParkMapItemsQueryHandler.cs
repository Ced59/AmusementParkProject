using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération optimisée des marqueurs publics d'un parc.
/// </summary>
public sealed class GetParkMapItemsQueryHandler : IQueryHandler<GetParkMapItemsQuery, ApplicationResult<ParkMapItemsResult>>
{
    private readonly IParkMapItemsReadRepository repository;

    public GetParkMapItemsQueryHandler(IParkMapItemsReadRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<ParkMapItemsResult>> HandleAsync(GetParkMapItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkMapItemsResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        ParkMapItemsResult? result = await this.repository.GetAsync(query.ParkId.Trim(), query.IncludeHidden, query.ClosedFilter, cancellationToken);
        if (result is null)
        {
            return ApplicationResult<ParkMapItemsResult>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        return ApplicationResult<ParkMapItemsResult>.Success(result);
    }
}
