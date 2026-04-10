using AmusementPark.Application.Errors;

namespace AmusementPark.WebAPI.Architecture
{
    /// <summary>
    /// Convertit les catégories d'erreur applicative vers des codes HTTP sans contaminer Application.
    /// </summary>
    public static class ApplicationResultHttpMapper
    {
        /// <summary>
        /// Retourne le code HTTP cible correspondant à une catégorie d'erreur applicative.
        /// </summary>
        public static int ToStatusCode(ApplicationErrorType errorType)
        {
            return errorType switch
            {
                ApplicationErrorType.Validation => StatusCodes.Status400BadRequest,
                ApplicationErrorType.NotFound => StatusCodes.Status404NotFound,
                ApplicationErrorType.RuleViolation => StatusCodes.Status422UnprocessableEntity,
                ApplicationErrorType.Conflict => StatusCodes.Status409Conflict,
                ApplicationErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ApplicationErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError,
            };
        }
    }
}
