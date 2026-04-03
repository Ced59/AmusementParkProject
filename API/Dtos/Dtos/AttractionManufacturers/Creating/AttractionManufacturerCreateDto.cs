using System.Collections.Generic;
using Common.General.Localization;
using System.ComponentModel.DataAnnotations;

namespace Dtos.AttractionManufacturers.Creating
{
    public class AttractionManufacturerCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}
