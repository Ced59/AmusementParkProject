using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class VisitTargetResolver : IVisitTargetResolver
{
    private readonly IVisitTargetReadRepository targetReadRepository;

    public VisitTargetResolver(IVisitTargetReadRepository targetReadRepository)
    {
        this.targetReadRepository = targetReadRepository;
    }

    public async Task<IReadOnlyDictionary<string, VisitTarget>> ResolveAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkItemIds);
        IReadOnlyCollection<VisitTarget> targets =
            await this.targetReadRepository.GetByIdsAsync(
                parkItemIds,
                cancellationToken);
        return targets.ToDictionary(
            static target => target.ParkItemId,
            static target => target,
            StringComparer.Ordinal);
    }
}
