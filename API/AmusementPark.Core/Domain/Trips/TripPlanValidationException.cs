namespace AmusementPark.Core.Domain.Trips;

public sealed class TripPlanValidationException : ArgumentException
{
    public TripPlanValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
