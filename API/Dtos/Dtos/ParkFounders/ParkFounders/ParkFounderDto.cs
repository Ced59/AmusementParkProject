using System.Collections.Generic;
using Common.General.Localization;

namespace Dtos.ParkFounders.ParkFounders
{
    public class ParkFounderDto
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}