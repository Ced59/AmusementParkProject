using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IVisitTargetResolver
{
    Task<IReadOnlyDictionary<string, VisitTarget>> ResolveAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);
}
