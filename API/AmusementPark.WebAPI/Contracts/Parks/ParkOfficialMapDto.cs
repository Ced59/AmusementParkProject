using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkOfficialMapDto
{
    public required string Id { get; set; }

    public int Year { get; set; }

    public required string Format { get; set; }

    public required string DocumentUrl { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeInBytes { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? SourcePageUrl { get; set; }

    public string? LanguageCode { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new List<LocalizedTextDto>();

    public List<LocalizedTextDto> AlternativeTexts { get; set; } = new List<LocalizedTextDto>();

    public DateTime? LastVerifiedAtUtc { get; set; }
}
