using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkEditorialScopeLoader
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;

    public HistoricalParkEditorialScopeLoader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<HistoricalParkEditorialScope?> LoadAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            return null;
        }

        Task<Park?> parkTask = this.parkRepository.GetByIdAsync(
            normalizedParkId,
            true,
            cancellationToken);
        Task<IReadOnlyCollection<ParkItem>> itemsTask = this.parkItemRepository.GetByParkIdAsync(
            normalizedParkId,
            true,
            cancellationToken);
        Task<IReadOnlyCollection<ParkZone>> zonesTask = this.parkZoneRepository.GetByParkIdAsync(
            normalizedParkId,
            cancellationToken);
        await Task.WhenAll(parkTask, itemsTask, zonesTask);

        Park? park = await parkTask;
        if (park is null)
        {
            return null;
        }

        IReadOnlyCollection<ParkItem> items = await itemsTask;
        IReadOnlyCollection<ParkZone> zones = await zonesTask;
        HistoricalSubject[] currentSubjects = new[]
            {
                new HistoricalSubject(
                    HistoricalSubjectType.Park,
                    park.Id,
                    ResolveLabel(park.Name, "Park"),
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                    park.Id),
            }
            .Concat(items.Select(item => new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                ResolveLabel(item.Name, "Park item"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
            .Concat(zones.Select(zone => new HistoricalSubject(
                HistoricalSubjectType.ParkZone,
                zone.Id,
                ResolveLabel(zone.Name, "Park zone"),
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id)))
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();

        return new HistoricalParkEditorialScope(
            park.Id,
            ResolveLabel(park.Name, "Park"),
            currentSubjects,
            zones.Select(static zone => zone.Id).Distinct().ToArray());
    }

    private static string ResolveLabel(string? label, string fallback)
    {
        return string.IsNullOrWhiteSpace(label) ? fallback : label.Trim();
    }
}
