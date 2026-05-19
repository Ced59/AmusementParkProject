namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Statut de revue interne utilisé uniquement par l'administration pour piloter la validation humaine
/// des données importées ou administrées. Il est volontairement indépendant de la visibilité publique.
/// </summary>
public enum AdminReviewStatus
{
    /// <summary>
    /// Donnée à vérifier par un administrateur. C'est le statut par défaut des données non validées,
    /// notamment celles issues d'une source externe.
    /// </summary>
    ToReview = 0,

    /// <summary>
    /// Donnée relue et validée par un administrateur.
    /// </summary>
    Validated = 1,

    /// <summary>
    /// Alias de compatibilité avec l'ancien nom M14. Ne pas utiliser dans le nouveau code.
    /// </summary>
    Ready = Validated,

    /// <summary>
    /// Donnée repoussée volontairement pour ne plus polluer le flux principal de traitement.
    /// </summary>
    ToProcessLater = 2,

    /// <summary>
    /// Donnée considérée comme non pertinente pour le site.
    /// </summary>
    NotRelevant = 3,
}
