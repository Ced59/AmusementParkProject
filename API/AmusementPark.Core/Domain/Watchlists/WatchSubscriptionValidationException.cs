namespace AmusementPark.Core.Domain.Watchlists;

public sealed class WatchSubscriptionValidationException : ArgumentException
{
    public WatchSubscriptionValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
