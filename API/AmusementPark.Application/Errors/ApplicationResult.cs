namespace AmusementPark.Application.Errors
{
    /// <summary>
    /// Représente le résultat non générique d'un cas d'usage applicatif.
    /// </summary>
    public class ApplicationResult
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="ApplicationResult"/>.
        /// </summary>
        /// <param name="errors">Erreurs applicatives éventuelles.</param>
        protected ApplicationResult(IReadOnlyCollection<ApplicationError> errors)
        {
            this.Errors = errors;
        }

        /// <summary>
        /// Obtient la collection d'erreurs applicatives.
        /// </summary>
        public IReadOnlyCollection<ApplicationError> Errors { get; }

        /// <summary>
        /// Obtient une valeur indiquant si l'opération a réussi.
        /// </summary>
        public bool IsSuccess => this.Errors.Count == 0;

        /// <summary>
        /// Construit un résultat de succès.
        /// </summary>
        public static ApplicationResult Success()
        {
            return new ApplicationResult(Array.Empty<ApplicationError>());
        }

        /// <summary>
        /// Construit un résultat d'échec.
        /// </summary>
        public static ApplicationResult Failure(params ApplicationError[] errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new ApplicationResult(errors);
        }

        /// <summary>
        /// Construit un résultat d'échec.
        /// </summary>
        public static ApplicationResult Failure(IReadOnlyCollection<ApplicationError> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new ApplicationResult(errors);
        }
    }
}
