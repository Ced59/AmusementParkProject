using System.Text.RegularExpressions;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>Matches the public card's stripHtml and NaturalTextTruncatorService formatting.</summary>
public static class PublicCardTextHttpMapper
{
    private const string JsWhitespace = @"[\u0009-\u000D\u0020\u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000\uFEFF]";

    public static string? ToCardDescription(string? value, int maxLength = 140, string ellipsis = "...", bool naturalBreak = true)
    {
        string text = Regex.Replace(value ?? string.Empty, @"<[^>]*>", " ");
        text = text.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
        text = Regex.Replace(text, JsWhitespace + "+", " ").Trim(' ');
        if (text.Length == 0)
        {
            return null;
        }
        if (text.Length <= maxLength)
        {
            return text;
        }
        int hardLimit = Math.Max(0, maxLength - ellipsis.Length);
        string candidate = text[..hardLimit].TrimEnd(' ');
        int breakpoint = candidate.LastIndexOfAny(new[] { ' ', ',', ';', ':', '.', '!', '?' });
        if (naturalBreak && breakpoint >= Math.Floor(hardLimit * 0.62))
        {
            return candidate[..breakpoint].TrimEnd(' ', ',', ';', ':', '.', '!', '?') + ellipsis;
        }
        return candidate + ellipsis;
    }
}
