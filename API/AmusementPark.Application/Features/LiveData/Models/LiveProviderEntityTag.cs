namespace AmusementPark.Application.Features.LiveData.Models;

public static class LiveProviderEntityTag
{
    public const int MaximumLength = 500;

    public static bool TryNormalize(string? value, out string? normalizedValue)
    {
        normalizedValue = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string candidate = value.Trim();
        if (candidate.Length > MaximumLength || candidate.Any(char.IsControl))
        {
            return false;
        }

        int openingQuoteIndex = candidate.StartsWith("W/", StringComparison.Ordinal) ? 2 : 0;
        if (candidate.Length < openingQuoteIndex + 2
            || candidate[openingQuoteIndex] != '"'
            || candidate[^1] != '"')
        {
            return false;
        }

        for (int index = openingQuoteIndex + 1; index < candidate.Length - 1; index++)
        {
            char character = candidate[index];
            if (character == '"'
                || character < '\u0021'
                || character == '\u007f'
                || character > '\u00ff')
            {
                return false;
            }
        }

        normalizedValue = candidate;
        return true;
    }
}
