using Common.General.Localization;
using System.ComponentModel.DataAnnotations;

namespace Dtos.ParkItems.Updating
{
    public class ParkItemUpdateDto
    {
        [Required]
        public string ParkId { get; set; } = string.Empty;

        public string? ZoneId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public ParkItemCategoryDto Category { get; set; }
        public ParkItemTypeDto Type { get; set; }
        public string? Subtype { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public bool IsVisible { get; set; } = true;
    }
}
