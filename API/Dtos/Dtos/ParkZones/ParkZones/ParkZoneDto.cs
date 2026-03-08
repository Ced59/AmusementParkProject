using Common.General.Localization;

namespace Dtos.ParkZones.ParkZones
{
    public class ParkZoneDto
    {
        public string? Id { get; set; }
        public string ParkId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public bool IsVisible { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
