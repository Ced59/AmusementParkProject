namespace Common.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Retourne le nom texte de n'importe quelle valeur d'énumération.
        /// </summary>
        public static string ToEnumString<TEnum>(this TEnum enumValue)
            where TEnum : struct, Enum
            => enumValue.ToString();

        /// <summary>
        /// Retourne le nom texte en minuscules de n'importe quelle valeur d'énumération.
        /// </summary>
        public static string ToEnumMinusString<TEnum>(this TEnum enumValue)
            where TEnum : struct, Enum
            => enumValue.ToEnumString().ToLowerInvariant();

        /// <summary>
        /// Convertit une chaîne en valeur d'énumération TEnum.
        /// Ignore la casse, et lève ArgumentException si la conversion échoue.
        /// </summary>
        public static TEnum ToEnum<TEnum>(this string value)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value), "La chaîne ne peut pas être nulle ou vide.");

            if (Enum.TryParse<TEnum>(value, ignoreCase: true, out TEnum result))
                return result;

            throw new ArgumentException(
                $"La valeur « {value} » n'est pas valide pour l'énumération {typeof(TEnum).Name}.",
                nameof(value));
        }

        /// <summary>
        /// Convertit n'importe quelle enum-source en enum-destination en se basant sur le nom de la valeur.
        /// </summary>
        public static TDestination MapTo<TSource, TDestination>(this TSource source)
            where TSource : struct, Enum
            where TDestination : struct, Enum
        {
            var name = source.ToString();
            if (Enum.TryParse<TDestination>(name, ignoreCase: true, out TDestination dest))
                return dest;

            throw new ArgumentException(
                $"Impossible de mapper la valeur « {name} » ({typeof(TSource).Name}) vers {typeof(TDestination).Name}");
        }
    }
}