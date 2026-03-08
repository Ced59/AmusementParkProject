using System.ComponentModel.DataAnnotations;
using Common.General.Localization;

namespace Dtos.ParkOperators.Creating;

public class ParkOperatorCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public List<LocalizedItem<string>> Description { get; set; } = new();
}