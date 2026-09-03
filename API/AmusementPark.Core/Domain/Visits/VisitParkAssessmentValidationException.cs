namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Erreur de validation de l'évaluation privée d'un parc pendant une visite.
/// </summary>
public sealed class VisitParkAssessmentValidationException : InvalidOperationException
{
    public VisitParkAssessmentValidationException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
