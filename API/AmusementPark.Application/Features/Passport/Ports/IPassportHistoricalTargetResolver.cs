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

    Task<PassportHistoricalTargetContext> ResolveRecordedAsync(
        Visit visit,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> ResolveMainImageIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);

    Task<PassportHistoricalTargetContext> ResolveAsync(
        string parkId,
        VisitDate visitDate,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<VisitDate, PassportHistoricalTargetContext>> ResolveManyAsync(
        string parkId,
        IReadOnlyCollection<VisitDate> visitDates,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);
}
