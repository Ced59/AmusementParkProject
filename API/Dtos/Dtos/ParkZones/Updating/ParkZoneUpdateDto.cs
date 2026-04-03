using System.Collections.Generic;
using Common.General.Localization;
using System.ComponentModel.DataAnnotations;

namespace Dtos.ParkZones.Updating
{
    public class ParkZoneUpdateDto
    {
        [Required]
        public string ParkId { get; set; } = string.Empty;

        // Legacy fallback for old callers.
        public string? Name { get; set; }

        public List<LocalizedItem<string>> Names { get; set; } = new();
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsVisible { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
