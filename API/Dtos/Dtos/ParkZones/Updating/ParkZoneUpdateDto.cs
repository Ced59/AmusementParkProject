using Common.General.Localization;
using System.ComponentModel.DataAnnotations;

namespace Dtos.ParkZones.Updating
{
    public class ParkZoneUpdateDto
    {
        [Required]
        public string ParkId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public bool IsVisible { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
