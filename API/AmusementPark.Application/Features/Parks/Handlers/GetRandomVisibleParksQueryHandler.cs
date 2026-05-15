using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération d'une sélection aléatoire de parcs publics pour la home.
/// </summary>
public sealed class GetRandomVisibleParksQueryHandler : IQueryHandler<GetRandomVisibleParksQuery, ApplicationResult<IReadOnlyCollection<Park>>>
{
    private const int DefaultLimit = 4;
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 4;

    private readonly IParkRepository parkRepository;

    public GetRandomVisibleParksQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Park>>> HandleAsync(GetRandomVisibleParksQuery query, CancellationToken cancellationToken = default)
    {
        int requestedLimit = query.Limit <= 0 ? DefaultLimit : query.Limit;
        int normalizedLimit = Math.Clamp(requestedLimit, MinimumLimit, MaximumLimit);

        IReadOnlyCollection<Park> parks = await this.parkRepository.GetRandomVisibleAsync(normalizedLimit, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Park>>.Success(parks);
    }
}
