namespace AmusementPark.Core.Domain.ParkFit;

public sealed class ParkFitGroupProfileValidationException : ArgumentException
{
    public ParkFitGroupProfileValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
