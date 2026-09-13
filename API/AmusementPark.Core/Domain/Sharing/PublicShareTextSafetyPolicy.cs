namespace AmusementPark.Core.Domain.Sharing;

public static class PublicShareTextSafetyPolicy
{
    private static readonly string[] ForbiddenFragments =
    {
        "<",
        ">",
        "http://",
        "https://",
        "www.",
        "javascript:",
        "vbscript:",
        "data:",
    };

    public static bool IsSafePlainText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return !value.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t')
            && !ForbiddenFragments.Any(fragment =>
                value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
