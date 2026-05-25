using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Type réutilisable de condition d'accès d'une attraction.
/// </summary>
public sealed class AttractionAccessConditionTypeDefinition
{
    /// <summary>
    /// Identifiant technique Mongo.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Clé métier stable utilisée par les conditions et les imports JSON.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Type enum historique associé quand il existe, sinon Custom.
    /// </summary>
    public AttractionAccessConditionType LegacyType { get; set; } = AttractionAccessConditionType.Custom;

    /// <summary>
    /// Indique si le type fait partie du catalogue système.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Indique si le type peut être proposé dans l'administration.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Libellés communs localisés du type.
    /// </summary>
    public List<LocalizedText> Labels { get; set; } = new();

    /// <summary>
    /// Descriptions communes localisées du type.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Ordre d'affichage dans les listes d'administration.
    /// </summary>
    public int SortOrder { get; set; }
}
