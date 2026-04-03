using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Common.General.Localization;

namespace Dtos.ParkOperators.Updating;

public class ParkOperatorUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public List<LocalizedItem<string>> Description { get; set; } = new();
}