namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalSourceDto
{
    public string Id { get; init; } = string.Empty;

    public int Revision { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string PublisherOrAuthor { get; init; } = string.Empty;

    public string? Url { get; init; }

    public string? BibliographicReference { get; init; }

    public DateOnly? PublishedOn { get; init; }

    public DateOnly AccessedOn { get; init; }

    public string? LanguageCode { get; init; }

    public string? ArchiveUrl { get; init; }

    public IReadOnlyCollection<string> Scopes { get; init; } = Array.Empty<string>();

    public string? AdminNote { get; init; }

    public string Accessibility { get; init; } = string.Empty;

    public string WorkflowState { get; init; } = string.Empty;

    public string PublicationState { get; init; } = string.Empty;
}
