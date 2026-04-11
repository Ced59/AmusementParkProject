using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP de mise à jour de tag d'image.
/// </summary>
public sealed class UpdateImageTagRequest
{
    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
