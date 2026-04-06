namespace AmusementPark.Application.Errors
{
    /// <summary>
    /// Représente le résultat générique d'un cas d'usage applicatif.
    /// </summary>
    /// <typeparam name="TValue">Type de valeur retournée en cas de succès.</typeparam>
    public sealed class ApplicationResult<TValue> : ApplicationResult
    {
        private ApplicationResult(TValue? value, IReadOnlyCollection<ApplicationError> errors)
            : base(errors)
        {
            this.Value = value;
        }

        /// <summary>
        /// Obtient la valeur de succès éventuelle.
        /// </summary>
        public TValue? Value { get; }

        /// <summary>
        /// Construit un résultat de succès.
        /// </summary>
        public static ApplicationResult<TValue> Success(TValue value)
        {
            return new ApplicationResult<TValue>(value, Array.Empty<ApplicationError>());
        }

        /// <summary>
        /// Construit un résultat d'échec.
        /// </summary>
        public static ApplicationResult<TValue> Failure(params ApplicationError[] errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new ApplicationResult<TValue>(default, errors);
        }

        /// <summary>
        /// Construit un résultat d'échec.
        /// </summary>
        public static ApplicationResult<TValue> Failure(IReadOnlyCollection<ApplicationError> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new ApplicationResult<TValue>(default, errors);
        }
    }
}
