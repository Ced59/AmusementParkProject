namespace AmusementPark.Core.Domain.Sharing;

public sealed class ProfileComparisonValidationException : InvalidOperationException
{
    public ProfileComparisonValidationException(string errorCode, string message)
        : base(message)
    {
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
