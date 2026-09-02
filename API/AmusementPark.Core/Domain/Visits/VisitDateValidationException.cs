namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Erreur de validation d'une date de visite partielle ou exacte.
/// </summary>
public sealed class VisitDateValidationException : ArgumentException
{
    public VisitDateValidationException(string errorCode, string message, string parameterName)
        : base(message, parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
