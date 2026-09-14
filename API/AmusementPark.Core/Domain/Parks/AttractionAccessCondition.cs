using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Contrainte d'accès d'une attraction.
/// </summary>
public sealed class AttractionAccessCondition
{
    public const int CurrentProvenanceSchemaVersion = 1;

    /// <summary>
    /// Type de contrainte.
    /// </summary>
    public AttractionAccessConditionType Type { get; set; }

    /// <summary>
    /// Clé du type réutilisable de condition d'accès.
    /// </summary>
    public string? TypeKey { get; set; }

    /// <summary>
    /// Indique si la contrainte est personnalisée.
    /// </summary>
    public bool? IsCustom { get; set; }

    /// <summary>
    /// Clé stable d'un type personnalisé de contrainte.
    /// </summary>
    public string? CustomTypeKey { get; set; }

    /// <summary>
    /// Libellés localisés du type personnalisé.
    /// </summary>
    public List<LocalizedText> CustomTypeLabel { get; set; } = new();

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

    /// <summary>
    /// Version du contrat de provenance embarqué.
    /// </summary>
    public int ProvenanceSchemaVersion { get; set; } = CurrentProvenanceSchemaVersion;

    /// <summary>
    /// Nature de la source qui atteste cette condition.
    /// </summary>
    public AttractionAccessConditionSourceKind SourceKind { get; set; } = AttractionAccessConditionSourceKind.Unknown;

    /// <summary>
    /// URL publique de la source, lorsqu'elle existe.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Référence interne ou éditoriale de la source.
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>
    /// Date UTC de collecte de la source.
    /// </summary>
    public DateTime? CollectedAtUtc { get; set; }

    /// <summary>
    /// Date UTC de dernière vérification éditoriale.
    /// </summary>
    public DateTime? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Langue du contenu source.
    /// </summary>
    public string? SourceLanguageCode { get; set; }

    /// <summary>
    /// Résumé fidèle et localisé de la règle attestée.
    /// </summary>
    public List<LocalizedText> SourceSummary { get; set; } = new();

    /// <summary>
    /// Confiance éditoriale accordée à la transcription.
    /// </summary>
    public AttractionAccessConditionConfidence SourceConfidence { get; set; } = AttractionAccessConditionConfidence.Unknown;

    /// <summary>
    /// Portée de la condition.
    /// </summary>
    public AttractionAccessConditionScope Scope { get; set; } = AttractionAccessConditionScope.Attraction;

    /// <summary>
    /// Précision de portée, obligatoire pour une portée autre que l'attraction entière.
    /// </summary>
    public string? ScopeDetail { get; set; }

    /// <summary>
    /// Premier jour local inclus pendant lequel la condition s'applique.
    /// </summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>
    /// Dernier jour local inclus pendant lequel la condition s'applique.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }
}
