namespace AmusementPark.Core.Domain.Sharing;

public sealed class ShareModerationValidationException : InvalidOperationException
{
    public ShareModerationValidationException(string errorCode, string message)
        : base(message)
    {
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
