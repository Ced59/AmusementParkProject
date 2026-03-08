using Common.General.Localization;

namespace Dtos.ParkItems.ParkItems
{
    public class ParkItemDto
    {
        public string? Id { get; set; }
        public string ParkId { get; set; } = string.Empty;
        public string? ZoneId { get; set; }
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
