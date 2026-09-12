namespace AmusementPark.Application.Features.Sharing.Services;

public static class PublicIdentityShareSourceScope
{
    private const string AvatarPrefix = "public-identity:avatar:";
    private const string DisplayNamePrefix = "public-identity:display-name:";

    public static string CreateAvatar(string ownerUserId)
    {
        return Create(AvatarPrefix, ownerUserId);
    }

    public static string CreateDisplayName(string ownerUserId)
    {
        return Create(DisplayNamePrefix, ownerUserId);
    }

    private static string Create(string prefix, string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(prefix, ownerUserId.Trim());
    }
}
