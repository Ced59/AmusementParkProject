using System.Collections.Generic;
using Common.General.Localization;

namespace Dtos.ParkZones.ParkZones
{
    public class ParkZoneDto
    {
        public string? Id { get; set; }
        public string ParkId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<LocalizedItem<string>> Names { get; set; } = new();
        public string? Slug { get; set; }
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsVisible { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
