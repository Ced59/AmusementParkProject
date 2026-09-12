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

    public static bool TryParse(string sourceScopeKey, out string ownerUserId)
    {
        ownerUserId = string.Empty;
        if (string.IsNullOrWhiteSpace(sourceScopeKey)
            || !sourceScopeKey.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string candidate = sourceScopeKey[Prefix.Length..].Trim();
        if (candidate.Length == 0)
        {
            return false;
        }

        ownerUserId = candidate;
        return true;
    }
}
