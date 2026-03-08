namespace Common.General.Localization
{
    public static class LocalizedItemExtensions
    {
        public static T? Resolve<T>(
            this IEnumerable<LocalizedItem<T>>? items,
            string? lang,
            string defaultLang = "en")
        {
            if (items is null)
            {
                return default;
            }

            string normalizedDefault = string.IsNullOrWhiteSpace(defaultLang)
                ? "en"
                : defaultLang.Trim().ToLowerInvariant();

            string normalizedLang = string.IsNullOrWhiteSpace(lang)
                ? normalizedDefault
                : lang.Trim().ToLowerInvariant();

            List<LocalizedItem<T>> safeItems = items
                .Where(item => item is not null)
                .Where(item => !string.IsNullOrWhiteSpace(item.LanguageCode))
                .ToList();

            LocalizedItem<T>? match = safeItems.FirstOrDefault(item =>
                string.Equals(item.LanguageCode, normalizedLang, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match.Value;
            }

            match = safeItems.FirstOrDefault(item =>
                string.Equals(item.LanguageCode, normalizedDefault, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match.Value;
            }

            LocalizedItem<T>? firstItem = safeItems.FirstOrDefault();

            if (firstItem is null)
            {
                return default;
            }

            return firstItem.Value;
        }
    }
}