namespace AmusementPark.Core.Domain.Watchlists;

public sealed class UserCollectionValidationException : ArgumentException
{
    public UserCollectionValidationException(string code, string message)
        : base(message)
    {
        this.Code = code;
    }

    public string Code { get; }
}
