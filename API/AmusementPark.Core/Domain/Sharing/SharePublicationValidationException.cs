namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Erreur de validation ou de transition d'une publication personnelle.
/// </summary>
public sealed class SharePublicationValidationException : InvalidOperationException
{
    public SharePublicationValidationException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
