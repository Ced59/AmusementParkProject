namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Statut de traitement interne utilisé par l'administration pour prioriser les listes.
/// </summary>
public enum AdminReviewStatus
{
    /// <summary>
    /// Élément traité normalement dans les listes d'administration.
    /// </summary>
    Ready = 0,

    /// <summary>
    /// Élément volontairement repoussé en fin de liste pour traitement ultérieur.
    /// </summary>
    ToProcessLater = 1,
}
