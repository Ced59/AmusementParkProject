using Common.General.Localization;

namespace Dtos.AttractionManufacturers.AttractionManufacturers
{
    public class AttractionManufacturerDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<LocalizedItem<string>> Biography { get; set; } = new();
        public int AttractionCount { get; set; }
    }
}
