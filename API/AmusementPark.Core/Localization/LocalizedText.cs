namespace AmusementPark.Core.Localization;

/// <summary>
/// Représente un texte localisé du domaine.
/// </summary>
public sealed class LocalizedText : LocalizedItem<string>
{
    /// <summary>
    /// Initialise une nouvelle instance vide de <see cref="LocalizedText"/>.
    /// </summary>
    public LocalizedText()
    {
    }

    /// <summary>
    /// Initialise une nouvelle instance de <see cref="LocalizedText"/>.
    /// </summary>
    /// <param name="languageCode">Code langue ISO.</param>
    /// <param name="value">Texte localisé.</param>
    public LocalizedText(string languageCode, string? value)
    {
        LanguageCode = languageCode;
        Value = value;
    }
}
