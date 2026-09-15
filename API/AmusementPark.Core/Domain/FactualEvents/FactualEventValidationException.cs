namespace AmusementPark.Core.Domain.FactualEvents;

public sealed class FactualEventValidationException : ArgumentException
{
    public FactualEventValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
