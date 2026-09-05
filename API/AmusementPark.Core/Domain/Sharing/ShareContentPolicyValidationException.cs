namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Erreur de validation d'une politique de contenu public.
/// </summary>
public sealed class ShareContentPolicyValidationException : ArgumentException
{
    public ShareContentPolicyValidationException(
        string errorCode,
        string message,
        string? parameterName = null)
        : base(message, parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
