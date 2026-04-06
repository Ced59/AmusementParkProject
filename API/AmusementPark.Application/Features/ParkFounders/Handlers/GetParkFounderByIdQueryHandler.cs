using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkFounders.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Handlers;

/// <summary>
/// Handler de récupération d'un park founder par identifiant.
/// </summary>
public sealed class GetParkFounderByIdQueryHandler : IQueryHandler<GetParkFounderByIdQuery, ApplicationResult<ParkFounder>>
{
    private readonly IParkFounderRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetParkFounderByIdQueryHandler"/>.
    /// </summary>
    public GetParkFounderByIdQueryHandler(IParkFounderRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkFounder>> HandleAsync(GetParkFounderByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationError.NotFound("park-founder.not-found", "Park founder not exists"));
        }

        ParkFounder? entity = await this.repository.GetByIdAsync(query.Id, cancellationToken);
        if (entity is null)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationError.NotFound("park-founder.not-found", "Park founder not exists"));
        }

        return ApplicationResult<ParkFounder>.Success(entity);
    }
}
