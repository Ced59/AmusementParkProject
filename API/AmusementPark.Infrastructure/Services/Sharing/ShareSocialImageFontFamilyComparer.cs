using SixLabors.Fonts;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageFontFamilyComparer
{
    private static readonly string[] PreferredUnicodeFamilies =
    {
        "Noto Sans CJK JP",
        "Noto Sans CJK SC",
        "Noto Sans CJK TC",
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
        if (index >= 0)
        {
            return index;
        }

        return familyName.StartsWith("Noto Sans", StringComparison.OrdinalIgnoreCase)
            ? PreferredUnicodeFamilies.Length
            : string.Equals(familyName, "DejaVu Sans", StringComparison.OrdinalIgnoreCase)
                ? PreferredUnicodeFamilies.Length + 1
                : PreferredUnicodeFamilies.Length + 2;
    }

    public static bool IsSupported(string familyName)
    {
        return string.Equals(familyName, "DejaVu Sans", StringComparison.OrdinalIgnoreCase)
            || (familyName.StartsWith("Noto Sans", StringComparison.OrdinalIgnoreCase)
                && !familyName.Contains("Emoji", StringComparison.OrdinalIgnoreCase));
    }
}
