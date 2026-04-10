using System.ComponentModel.DataAnnotations;
using Common.General.Localization;

namespace Dtos.ParkFounders.Creating
{
    public class ParkFounderCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}