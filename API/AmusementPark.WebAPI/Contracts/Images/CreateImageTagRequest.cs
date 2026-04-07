using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Contrat HTTP de création de tag d'image.
/// </summary>
public sealed class CreateImageTagRequest
{
    public string Slug { get; set; } = string.Empty;

    public List<LocalizedTextDto> Labels { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();
}
