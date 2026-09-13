using SixLabors.Fonts;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageFontFamilyComparer
{
    private static readonly string[] PreferredUnicodeFamilies =
    {
        "Noto Sans CJK JP",
        "Noto Sans CJK SC",
        "Noto Sans CJK TC",
        "Noto Sans",
        "DejaVu Sans",
    };

    public static int GetPriority(FontFamily family)
    {
        return GetPriority(family.Name);
    }

    public static int GetPriority(string familyName)
    {
        int index = Array.FindIndex(
            PreferredUnicodeFamilies,
            name => string.Equals(name, familyName, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : PreferredUnicodeFamilies.Length;
    }
}
