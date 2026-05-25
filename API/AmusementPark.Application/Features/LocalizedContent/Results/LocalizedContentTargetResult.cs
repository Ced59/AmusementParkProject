namespace AmusementPark.Application.Features.LocalizedContent.Results;

/// <summary>
/// Cible localisable sélectionnable dans l'administration.
/// </summary>
public sealed record LocalizedContentTargetResult(
    string EntityType,
    string EntityId,
    string Label,
    string? Context,
    IReadOnlyCollection<string> SupportedFields);
