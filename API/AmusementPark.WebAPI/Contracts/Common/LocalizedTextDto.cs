using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Contrat HTTP d'une valeur localisée.
/// </summary>
public sealed class LocalizedTextDto
{
    /// <summary>
    /// Code langue ISO.
    /// </summary>
    [Required]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Valeur localisée.
    /// </summary>
    public string? Value { get; set; }
}
