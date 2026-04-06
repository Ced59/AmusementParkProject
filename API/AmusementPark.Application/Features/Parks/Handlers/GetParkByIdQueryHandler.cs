using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération d'un parc par identifiant.
/// </summary>
public sealed class GetParkByIdQueryHandler : IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;

    public GetParkByIdQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(GetParkByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId, query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.EntityNotFound("Park", query.ParkId));
        }

        return ApplicationResult<Park>.Success(park);
    }
}
