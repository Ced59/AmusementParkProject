using AmusementPark.Application.Common.Contracts;

namespace AmusementPark.Application.Features.Videos.Contracts;

public sealed class VideoTagWriteModel
{
    public string Slug { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextValue> Labels { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<LocalizedTextValue> Descriptions { get; init; } = Array.Empty<LocalizedTextValue>();

    public bool IsActive { get; init; } = true;
}
