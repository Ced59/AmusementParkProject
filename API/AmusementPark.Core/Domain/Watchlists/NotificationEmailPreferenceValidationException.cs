namespace AmusementPark.Core.Domain.Watchlists;

public sealed class NotificationEmailPreferenceValidationException : ArgumentException
{
    public NotificationEmailPreferenceValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
