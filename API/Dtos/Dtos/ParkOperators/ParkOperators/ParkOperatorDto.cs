using Common.General.Localization;

namespace Dtos.ParkOperators.ParkOperators
{
    public class ParkOperatorDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<LocalizedItem<string>> Description { get; set; } = new();
    }
}