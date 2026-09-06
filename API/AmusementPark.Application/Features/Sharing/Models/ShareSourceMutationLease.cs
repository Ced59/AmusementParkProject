namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareSourceMutationLease
{
    public ShareSourceMutationLease(
        string scopeKey,
        string token,
        CancellationToken leaseCancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
        {
            throw new ArgumentException("A share source scope key is required.", nameof(scopeKey));
        }

        if (!Guid.TryParseExact(token, "N", out Guid parsedToken))
        {
            throw new ArgumentException("The share source mutation token is invalid.", nameof(token));
        }

        this.ScopeKey = scopeKey.Trim();
        this.Token = parsedToken.ToString("N");
        this.LeaseCancellationToken = leaseCancellationToken;
    }

    public string ScopeKey { get; }

    public string Token { get; }

    public CancellationToken LeaseCancellationToken { get; }

    public static ShareSourceMutationLease Create(
        string scopeKey,
        CancellationToken leaseCancellationToken = default)
    {
        return new ShareSourceMutationLease(
            scopeKey,
            Guid.NewGuid().ToString("N"),
            leaseCancellationToken);
    }
}
