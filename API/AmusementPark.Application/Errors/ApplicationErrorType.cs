namespace AmusementPark.Application.Errors
{
    /// <summary>
    /// Représente la nature d'une erreur applicative indépendante du transport HTTP.
    /// </summary>
    public enum ApplicationErrorType
    {
        /// <summary>
        /// La requête applicative est invalide.
        /// </summary>
        Validation = 1,

        /// <summary>
        /// La ressource demandée n'a pas été trouvée.
        /// </summary>
        NotFound = 2,

        /// <summary>
        /// Une règle métier ou un invariant bloque l'opération.
        /// </summary>
        RuleViolation = 3,

        /// <summary>
        /// L'opération entre en conflit avec l'état courant.
        /// </summary>
        Conflict = 4,

        /// <summary>
        /// L'utilisateur n'est pas authentifié.
        /// </summary>
        Unauthorized = 5,

        /// <summary>
        /// L'utilisateur est authentifié mais n'a pas les droits nécessaires.
        /// </summary>
        Forbidden = 6,

        /// <summary>
        /// Une erreur technique non prévue est survenue.
        /// </summary>
        Technical = 7,
    }
}
