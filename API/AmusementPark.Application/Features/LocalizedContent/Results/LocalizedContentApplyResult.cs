namespace AmusementPark.Application.Features.LocalizedContent.Results;

/// <summary>
/// Résultat d'application d'un JSON de contenu localisé.
/// </summary>
public sealed record LocalizedContentApplyResult(
    string EntityType,
    string EntityId,
    IReadOnlyCollection<string> UpdatedFields,
    int UpdatedLocalizedValueCount);
