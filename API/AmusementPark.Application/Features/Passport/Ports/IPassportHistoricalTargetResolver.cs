using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportHistoricalTargetResolver
{
    Task<PassportHistoricalTargetContext> ResolveAllAsync(
        Visit visit,
        CancellationToken cancellationToken);

    Task<PassportHistoricalTargetContext> ResolveAsync(
        Visit visit,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);

    Task<PassportHistoricalTargetContext> ResolveAsync(
        string parkId,
        VisitDate visitDate,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);
}
