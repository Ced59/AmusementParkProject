namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Centralise les seuils minimaux des pages publiques de collection qui peuvent etre indexees.
/// Les memes seuils sont reproduits dans la politique frontend afin que les balises robots et
/// les sitemaps restent alignes.
/// </summary>
public static class SeoPageValuePolicy
{
    public const int MinimumImageGalleryEntries = 3;

    public const int MinimumCollectionEntries = 2;

    public static bool IsImageGalleryIndexable(int entryCount)
    {
        return entryCount >= MinimumImageGalleryEntries;
    }

    public static bool IsCollectionIndexable(int entryCount)
    {
        return entryCount >= MinimumCollectionEntries;
    }
}
