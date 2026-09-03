namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Erreur de validation de l'évaluation privée d'une occurrence de ride.
/// </summary>
public sealed class RideAssessmentValidationException : InvalidOperationException
{
    public RideAssessmentValidationException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
