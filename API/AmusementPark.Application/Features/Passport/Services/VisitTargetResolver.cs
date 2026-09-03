using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class VisitTargetResolver : IVisitTargetResolver
{
    private readonly IParkItemRepository parkItemRepository;

    public VisitTargetResolver(IParkItemRepository parkItemRepository)
    {
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<IReadOnlyDictionary<string, VisitTarget>> ResolveAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkItemIds);
        IReadOnlyCollection<ParkItem> parkItems =
            await this.parkItemRepository.GetByIdsAsync(
                parkItemIds,
                cancellationToken);
        return parkItems.ToDictionary(
            static parkItem => parkItem.Id,
            static parkItem => new VisitTarget(
                parkItem.Id,
                parkItem.ParkId,
                parkItem.Name,
                parkItem.Category,
                ToDateOnly(parkItem.AttractionDetails?.OpeningDate),
                ToDateOnly(parkItem.AttractionDetails?.ClosingDate)),
            StringComparer.Ordinal);
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }
}
