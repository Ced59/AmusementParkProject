namespace AmusementPark.Core.Domain.Watchlists;

public sealed class UserNotificationValidationException : ArgumentException
{
    public UserNotificationValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
