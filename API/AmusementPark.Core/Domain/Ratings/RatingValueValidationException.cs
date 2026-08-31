namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Erreur de validation d'une note du domaine.
/// </summary>
public sealed class RatingValueValidationException : ArgumentException
{
    public RatingValueValidationException(string errorCode, string message, string parameterName)
        : base(message, parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
