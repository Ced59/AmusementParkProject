using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Contrainte d'accès d'une attraction.
/// </summary>
public sealed class AttractionAccessCondition
{
    /// <summary>
    /// Type de contrainte.
    /// </summary>
    public AttractionAccessConditionType Type { get; set; }

    /// <summary>
    /// Indique si la contrainte est personnalisée.
    /// </summary>
    public bool? IsCustom { get; set; }

    /// <summary>
    /// Valeur numérique éventuelle de la contrainte.
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Unité associée.
    /// </summary>
    public AttractionAccessConditionUnit? Unit { get; set; }

    /// <summary>
    /// Indique si un accompagnement est requis.
    /// </summary>
    public bool? RequiresAccompaniment { get; set; }

    /// <summary>
    /// Âge minimum de l'accompagnant.
    /// </summary>
    public int? MinimumCompanionAge { get; set; }

    /// <summary>
    /// Libellés localisés.
    /// </summary>
    public List<LocalizedText> Label { get; set; } = new();

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Description { get; set; } = new();

    /// <summary>
    /// Ordre d'affichage.
    /// </summary>
    public int? DisplayOrder { get; set; }
}
