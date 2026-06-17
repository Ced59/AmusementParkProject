using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Videos;

public sealed class VideoTag : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;

    public List<LocalizedText> Labels { get; set; } = new();

    public List<LocalizedText> Descriptions { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
