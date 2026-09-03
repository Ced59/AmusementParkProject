namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Erreur de validation ou de transition d'une visite.
/// </summary>
public sealed class VisitValidationException : InvalidOperationException
{
    public VisitValidationException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
