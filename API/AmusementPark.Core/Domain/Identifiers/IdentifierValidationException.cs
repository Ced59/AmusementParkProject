namespace AmusementPark.Core.Domain.Identifiers;

/// <summary>
/// Erreur de validation d'un identifiant métier opaque.
/// </summary>
public sealed class IdentifierValidationException : ArgumentException
{
    public IdentifierValidationException(string errorCode, string message, string parameterName)
        : base(message, parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
