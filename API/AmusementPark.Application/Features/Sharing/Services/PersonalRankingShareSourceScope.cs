namespace AmusementPark.Application.Features.Sharing.Services;

public static class PersonalRankingShareSourceScope
{
    private const string Prefix = "personal-ranking:";

    public const string PublicCatalog = "personal-ranking:public-catalog";

    public static string Create(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(Prefix, ownerUserId.Trim());
    }
}
