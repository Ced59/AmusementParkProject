using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkZones;

public sealed class ParkExplorerBucketDto
{
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<LocalizedTextDto> Names { get; set; } = new();

    public string? Slug { get; set; }

    public bool IsVirtual { get; set; }

    public int TotalItems { get; set; }

    public List<ParkZoneSummaryCountDto> CountsByCategory { get; set; } = new();

    public List<ParkZoneSummaryCountDto> CountsByType { get; set; } = new();
}
