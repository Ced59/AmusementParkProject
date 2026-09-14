using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkFit.Services;

/// <summary>
/// Compare le contenu localisé d'une preuve sans dépendre de l'identité objet.
/// </summary>
internal sealed class LocalizedTextComparer : IEqualityComparer<LocalizedText>
{
    public static LocalizedTextComparer Instance { get; } = new();

    private LocalizedTextComparer()
    {
    }

    public bool Equals(LocalizedText? left, LocalizedText? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(left.LanguageCode, right.LanguageCode, StringComparison.Ordinal)
            && string.Equals(left.Value, right.Value, StringComparison.Ordinal);
    }

    public int GetHashCode(LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(text.LanguageCode),
            text.Value is null ? 0 : StringComparer.Ordinal.GetHashCode(text.Value));
    }
}
