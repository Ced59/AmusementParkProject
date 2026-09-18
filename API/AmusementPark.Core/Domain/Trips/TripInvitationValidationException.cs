namespace AmusementPark.Core.Domain.Trips;

public sealed class TripInvitationValidationException : Exception
{
    public TripInvitationValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
