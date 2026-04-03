using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common.General.Localization;

namespace Dtos.ParkFounders.Updating
{
    public class ParkFounderUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}