using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;

public sealed class CreateParkDataEditorTokenRequestDto
{
    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string Label { get; set; } = string.Empty;

    [Range(1, 90)]
    public int ExpiresInDays { get; set; } = 30;
}
