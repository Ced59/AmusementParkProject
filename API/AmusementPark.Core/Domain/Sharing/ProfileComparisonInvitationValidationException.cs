namespace AmusementPark.Core.Domain.Sharing;

public sealed class ProfileComparisonInvitationValidationException : InvalidOperationException
{
    public ProfileComparisonInvitationValidationException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
