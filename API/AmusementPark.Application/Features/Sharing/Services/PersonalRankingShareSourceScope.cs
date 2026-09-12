namespace AmusementPark.Application.Features.Sharing.Services;

public static class PersonalRankingShareSourceScope
{
    private const string Prefix = "personal-ranking:";
    private const string RatingPrefix = "passport-profile-rating:";

    public const string PublicCatalog = "personal-ranking:public-catalog";

    public static string Create(string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(Prefix, ownerUserId.Trim());
    }

    public static string CreateRating(string ownerUserId, string selectionKey)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        string normalizedSelectionKey = selectionKey?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0)
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        if (normalizedSelectionKey.Length == 0)
        {
            throw new ArgumentException("A rating selection key is required.", nameof(selectionKey));
        }

        return string.Concat(
            RatingPrefix,
            Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(normalizedOwner)),
            ":",
            Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(normalizedSelectionKey)));
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
