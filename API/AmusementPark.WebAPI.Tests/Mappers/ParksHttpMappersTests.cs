using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkZones;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class ParksHttpMappersTests
{
    [Fact]
    public void ToHttp_WhenExplorerHasZones_ShouldKeepCountsAndUnassignedBucket()
    {
        ParkExplorerResult result = new()
        {
            ParkId = "park-1",
            Zones =
            [
                new ParkZone { Id = "zone-b", Name = "Zone B", SortOrder = 2 },
                new ParkZone { Id = "zone-a", Name = "Zone A", SortOrder = 1 },
            ],
            Items =
            [
                CreateItem("zone-a", ParkItemCategory.Attraction, ParkItemType.RollerCoaster),
                CreateItem("zone-a", ParkItemCategory.Restaurant, ParkItemType.Restaurant),
                CreateItem("zone-b", ParkItemCategory.Attraction, ParkItemType.DarkRide),
                CreateItem(null, ParkItemCategory.Service, ParkItemType.Toilets),
                CreateItem(" ", ParkItemCategory.Shop, ParkItemType.Shop),
            ],
        };

        ParkExplorerDto dto = result.ToHttp();

        Assert.Equal("park-1", dto.ParkId);
        Assert.True(dto.HasZones);
        Assert.Equal(5, dto.Overview.TotalItems);
        AssertCountKeys(dto.Overview.CountsByCategory, "Attraction", "Restaurant", "Service", "Shop");
        Assert.Collection(
            dto.Zones,
            zone =>
            {
                Assert.Equal("zone-a", zone.Id);
                Assert.Equal(2, zone.TotalItems);
                AssertCountKeys(zone.CountsByCategory, "Attraction", "Restaurant");
                AssertCountKeys(zone.CountsByType, "Restaurant", "RollerCoaster");
            },
            zone =>
            {
                Assert.Equal("zone-b", zone.Id);
                Assert.Equal(1, zone.TotalItems);
                AssertCountKeys(zone.CountsByCategory, "Attraction");
                AssertCountKeys(zone.CountsByType, "DarkRide");
            });
        Assert.NotNull(dto.Unassigned);
        Assert.Equal(2, dto.Unassigned.TotalItems);
        AssertCountKeys(dto.Unassigned.CountsByCategory, "Service", "Shop");
    }

    [Fact]
    public void ToHttp_WhenExplorerHasNoZonesAndNoUnassignedItems_ShouldReuseOverviewAsUnassignedBucket()
    {
        ParkExplorerResult result = new()
        {
            ParkId = "park-1",
            Items =
            [
                CreateItem("missing-zone", ParkItemCategory.Attraction, ParkItemType.FlatRide),
            ],
        };

        ParkExplorerDto dto = result.ToHttp();

        Assert.False(dto.HasZones);
        Assert.Same(dto.Overview, dto.Unassigned);
        Assert.Equal(1, dto.Unassigned?.TotalItems);
    }

    private static ParkItem CreateItem(string? zoneId, ParkItemCategory category, ParkItemType type)
    {
        return new ParkItem
        {
            ParkId = "park-1",
            ZoneId = zoneId,
            Name = Guid.NewGuid().ToString("N"),
            Category = category,
            Type = type,
        };
    }

    private static void AssertCountKeys(IReadOnlyCollection<ParkZoneSummaryCountDto> counts, params string[] keys)
    {
        Assert.Equal(keys, counts.Select(static count => count.Key).ToArray());
    }
}
