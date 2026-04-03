namespace Common.General.Localization
{
    public static class LocalizedItemExtensions
    {
        public static T? Resolve<T>(
            this IEnumerable<LocalizedItem<T>> items,
            string lang,
            string defaultLang = "en")
        {
            string normalizedLang = lang.ToLowerInvariant();
            string normalizedDefault = defaultLang.ToLowerInvariant();

            // 1. langue demandée
            LocalizedItem<T>? match = items
                .FirstOrDefault(i =>
                    i.LanguageCode.Equals(normalizedLang, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match.Value;
            }

            // 2. langue par défaut
            match = items.FirstOrDefault(i =>
                i.LanguageCode.Equals(normalizedDefault, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match.Value;
            }

            // 3. fallback : premier dispo
            return items.FirstOrDefault().Value;
        }
    }
}