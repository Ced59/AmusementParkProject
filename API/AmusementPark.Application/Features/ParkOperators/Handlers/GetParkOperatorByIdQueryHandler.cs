using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkOperators.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de récupération d'un park operator par identifiant.
/// </summary>
public sealed class GetParkOperatorByIdQueryHandler : IQueryHandler<GetParkOperatorByIdQuery, ApplicationResult<ParkOperator>>
{
    private readonly IParkOperatorRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetParkOperatorByIdQueryHandler"/>.
    /// </summary>
    public GetParkOperatorByIdQueryHandler(IParkOperatorRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkOperator>> HandleAsync(GetParkOperatorByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.Required(nameof(query.Id)));
        }

        ParkOperator? entity = await this.repository.GetByIdAsync(query.Id, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.EntityNotFound("ParkOperator", query.Id));
        }

        return ApplicationResult<ParkOperator>.Success(entity);
    }
}
