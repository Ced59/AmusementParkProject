namespace AmusementPark.Application.Features.Sharing.Services;

public static class PublicIdentityShareSourceScope
{
    private const string Prefix = "public-identity:";

    public static string Create(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(Prefix, ownerUserId.Trim());
    }
}
