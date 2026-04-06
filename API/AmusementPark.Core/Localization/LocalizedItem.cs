namespace AmusementPark.Core.Localization;

/// <summary>
/// Représente une valeur localisée indépendante de la couche de persistance.
/// </summary>
/// <typeparam name="T">Type de valeur transportée.</typeparam>
public sealed class LocalizedItem<T>
{
    /// <summary>
    /// Code langue ISO porté par la valeur.
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Valeur localisée.
    /// </summary>
    public T? Value { get; set; }
}
