namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalPersistenceValidationException : ArgumentException
{
    public HistoricalPersistenceValidationException(
        string errorCode,
        string message,
        string? parameterName = null)
        : base(message, parameterName)
    {
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
