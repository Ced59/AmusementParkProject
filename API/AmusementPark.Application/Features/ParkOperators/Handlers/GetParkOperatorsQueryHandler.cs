using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOperators.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de récupération de la liste des park operators.
/// </summary>
public sealed class GetParkOperatorsQueryHandler : IQueryHandler<GetParkOperatorsQuery, ApplicationResult<IReadOnlyCollection<ParkOperator>>>
{
    private readonly IParkOperatorRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetParkOperatorsQueryHandler"/>.
    /// </summary>
    public GetParkOperatorsQueryHandler(IParkOperatorRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<IReadOnlyCollection<ParkOperator>>> HandleAsync(GetParkOperatorsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ParkOperator> entities = await this.repository.GetAllAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkOperator>>.Success(entities);
    }
}
