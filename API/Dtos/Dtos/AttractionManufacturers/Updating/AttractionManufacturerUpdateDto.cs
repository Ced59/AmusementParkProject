using Common.General.Localization;
using System.ComponentModel.DataAnnotations;

namespace Dtos.AttractionManufacturers.Updating
{
    public class AttractionManufacturerUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public List<LocalizedItem<string>> Biography { get; set; } = new();
    }
}
