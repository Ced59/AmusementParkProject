using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkFounders.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Handlers;

/// <summary>
/// Handler de récupération de la liste des park founders.
/// </summary>
public sealed class GetParkFoundersQueryHandler : IQueryHandler<GetParkFoundersQuery, ApplicationResult<IReadOnlyCollection<ParkFounder>>>
{
    private readonly IParkFounderRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="GetParkFoundersQueryHandler"/>.
    /// </summary>
    public GetParkFoundersQueryHandler(IParkFounderRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<IReadOnlyCollection<ParkFounder>>> HandleAsync(GetParkFoundersQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ParkFounder> entities = await this.repository.GetAllAsync(cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkFounder>>.Success(entities);
    }
}
