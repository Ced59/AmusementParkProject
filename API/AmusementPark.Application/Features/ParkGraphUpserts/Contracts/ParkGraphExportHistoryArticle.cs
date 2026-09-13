using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportHistoryArticle
{
    public string? Slug { get; init; }

    public List<LocalizedText> Titles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Subtitles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> Summaries { get; init; } = new List<LocalizedText>();

    public string? MainImageId { get; init; }

    public List<ParkGraphExportHistoryArticleBlock> Blocks { get; init; } = new List<ParkGraphExportHistoryArticleBlock>();

    public List<ParkGraphExportHistorySource> Sources { get; init; } = new List<ParkGraphExportHistorySource>();

    public bool IsPublished { get; init; }
}
