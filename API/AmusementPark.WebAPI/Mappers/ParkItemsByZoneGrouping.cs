using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.ParkZones;

namespace AmusementPark.WebAPI.Mappers;

internal sealed class ParkItemsByZoneGrouping
{
    public ParkItemsByZoneGrouping(Dictionary<string, List<ParkItem>> itemsByZoneId, List<ParkItem> unassignedItems)
    {
        this.ItemsByZoneId = itemsByZoneId;
        this.UnassignedItems = unassignedItems;
    }

    public Dictionary<string, List<ParkItem>> ItemsByZoneId { get; }

    public List<ParkItem> UnassignedItems { get; }
}
