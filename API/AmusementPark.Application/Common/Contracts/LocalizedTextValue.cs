namespace AmusementPark.Application.Common.Contracts;

/// <summary>
/// Valeur localisée transport-agnostique utilisée par les commandes et requêtes applicatives.
/// </summary>
/// <param name="LanguageCode">Code langue.</param>
/// <param name="Value">Valeur localisée.</param>
public sealed record LocalizedTextValue(string LanguageCode, string Value);
