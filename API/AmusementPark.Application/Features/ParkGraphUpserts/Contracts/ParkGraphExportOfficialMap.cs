using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportOfficialMap
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public int Year { get; init; }

    public ParkOfficialMapFormat Format { get; init; }

    public string? DocumentUrl { get; init; }

    public string? StorageKey { get; init; }

    public string? OriginalFileName { get; init; }

    public string? ContentType { get; init; }

    public long? SizeInBytes { get; init; }

    public string? PreviewImageUrl { get; init; }

    public string? SourcePageUrl { get; init; }

    public string? LanguageCode { get; init; }

    public List<LocalizedText> Titles { get; init; } = new List<LocalizedText>();

    public List<LocalizedText> AlternativeTexts { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }
}
