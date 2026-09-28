using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalSourceDraftInput(
    HistoricalSourceType Type,
    string Title,
    string PublisherOrAuthor,
    string? Url,
    string? BibliographicReference,
    DateOnly? PublishedOn,
    DateOnly AccessedOn,
    string? LanguageCode,
    string? ArchiveUrl,
    IReadOnlyCollection<HistoricalSourceScope> Scopes,
    string? AdminNote,
    HistoricalSourceAccessibility Accessibility);
