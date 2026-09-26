namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalSourceDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string PublisherOrAuthor { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? BibliographicReference { get; set; }

    public DateOnly? PublishedOn { get; set; }

    public DateOnly AccessedOn { get; set; }

    public string? LanguageCode { get; set; }

    public string? ArchiveUrl { get; set; }

    public string Accessibility { get; set; } = string.Empty;
}
