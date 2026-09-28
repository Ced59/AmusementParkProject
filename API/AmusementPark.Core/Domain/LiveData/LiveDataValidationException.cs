namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveDataValidationException : ArgumentException
{
    public LiveDataValidationException(
        string code,
        string message,
        string? parameterName = null)
        : base(message, parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        this.Code = code;
    }

    public string Code { get; }
}
