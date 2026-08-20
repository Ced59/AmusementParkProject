using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Users;

/// <summary>
/// Request used to update the authenticated user's preferred language.
/// </summary>
public sealed class PreferredLanguageUpdateDto
{
    [Required]
    public string PreferredLanguage { get; set; } = string.Empty;
}
