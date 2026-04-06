namespace AmusementPark.Application.Errors
{
    /// <summary>
    /// Représente une erreur applicative transport-agnostique.
    /// </summary>
    /// <param name="Code">Code stable exploitable par les couches supérieures.</param>
    /// <param name="Message">Message fonctionnel ou technique.</param>
    /// <param name="Type">Catégorie d'erreur applicative.</param>
    /// <param name="Details">Détails complémentaires optionnels.</param>
    public sealed record ApplicationError(
        string Code,
        string Message,
        ApplicationErrorType Type,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Details = null)
    {
        /// <summary>
        /// Crée une erreur de validation.
        /// </summary>
        public static ApplicationError Validation(
            string code,
            string message,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>>? details = null)
        {
            return new ApplicationError(code, message, ApplicationErrorType.Validation, details);
        }

        /// <summary>
        /// Crée une erreur de ressource absente.
        /// </summary>
        public static ApplicationError NotFound(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.NotFound);
        }

        /// <summary>
        /// Crée une erreur de règle métier.
        /// </summary>
        public static ApplicationError RuleViolation(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.RuleViolation);
        }

        /// <summary>
        /// Crée une erreur de conflit.
        /// </summary>
        public static ApplicationError Conflict(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.Conflict);
        }

        /// <summary>
        /// Crée une erreur d'authentification.
        /// </summary>
        public static ApplicationError Unauthorized(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.Unauthorized);
        }

        /// <summary>
        /// Crée une erreur d'autorisation.
        /// </summary>
        public static ApplicationError Forbidden(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.Forbidden);
        }

        /// <summary>
        /// Crée une erreur technique.
        /// </summary>
        public static ApplicationError Technical(string code, string message)
        {
            return new ApplicationError(code, message, ApplicationErrorType.Technical);
        }
    }
}
