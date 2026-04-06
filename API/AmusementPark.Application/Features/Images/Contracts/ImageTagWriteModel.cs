using AmusementPark.Application.Common.Contracts;

namespace AmusementPark.Application.Features.Images.Contracts;

/// <summary>
/// Données applicatives d'écriture d'un tag d'image.
/// </summary>
public sealed class ImageTagWriteModel
{
    public string Slug { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextValue> Labels { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<LocalizedTextValue> Descriptions { get; init; } = Array.Empty<LocalizedTextValue>();

    public bool IsActive { get; init; } = true;
}
