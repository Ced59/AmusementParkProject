using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportHistoryArticleBlock
{
    public string Id { get; init; } = string.Empty;

    public HistoryArticleBlockType Type { get; init; }

    public int SortOrder { get; init; }

    public int? HeadingLevel { get; init; }

    public List<LocalizedText> Texts { get; init; } = new List<LocalizedText>();

    public string? ImageId { get; init; }

    public List<string> ImageIds { get; init; } = new List<string>();

    public List<LocalizedText> Captions { get; init; } = new List<LocalizedText>();
}
