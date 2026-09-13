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

internal sealed class ParkExplorerBucketCounts
{
    public ParkExplorerBucketCounts(int totalItems, Dictionary<string, int> countsByCategory, Dictionary<string, int> countsByType)
    {
        this.TotalItems = totalItems;
        this.CountsByCategory = countsByCategory;
        this.CountsByType = countsByType;
    }

    public int TotalItems { get; }

    public Dictionary<string, int> CountsByCategory { get; }

    public Dictionary<string, int> CountsByType { get; }
}
