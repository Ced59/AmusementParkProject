using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkZones;

public sealed class ParkZoneCreateDto
{
    [Required]
    public string ParkId { get; set; } = string.Empty;

    public string? Name { get; set; }

    public List<LocalizedTextDto> Names { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool IsVisible { get; set; } = true;

    public int SortOrder { get; set; }
}
