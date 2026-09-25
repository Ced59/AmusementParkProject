namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalTemporalValidationException : ArgumentException
{
    public HistoricalTemporalValidationException(
        string errorCode,
        string message,
        string? parameterName = null)
        : base(message, parameterName)
    {
        this.ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
