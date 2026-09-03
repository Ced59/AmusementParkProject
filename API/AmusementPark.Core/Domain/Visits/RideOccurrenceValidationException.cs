namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Erreur de validation ou de transition d'une occurrence de ride.
/// </summary>
public sealed class RideOccurrenceValidationException : InvalidOperationException
{
    public RideOccurrenceValidationException(
        string errorCode,
        string message,
        string? parameterName = null)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
        this.ParameterName = parameterName;
    }

    public string ErrorCode { get; }

    public string? ParameterName { get; }
}
