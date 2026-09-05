using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Version annuelle d'une carte publiée par le parc ou son exploitant officiel.
/// </summary>
public sealed class ParkOfficialMap
{
    public string Id { get; set; } = string.Empty;

    public int Year { get; set; }

    public ParkOfficialMapFormat Format { get; set; }

    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Clé opaque d'un fichier conservé dans le stockage objet applicatif.
    /// </summary>
    public string? StorageKey { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeInBytes { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? SourcePageUrl { get; set; }

    public string? LanguageCode { get; set; }

    public List<LocalizedText> Titles { get; set; } = new List<LocalizedText>();

    public List<LocalizedText> AlternativeTexts { get; set; } = new List<LocalizedText>();

    public bool IsVisible { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public bool IsPubliclyDisplayable()
    {
        return this.IsVisible
            && !string.IsNullOrWhiteSpace(this.Id)
            && this.Year >= 1800
            && (IsAbsoluteHttpUrl(this.DocumentUrl) || !string.IsNullOrWhiteSpace(this.StorageKey))
            && (!this.Format.Equals(ParkOfficialMapFormat.Image) || HasAccessibleAlternativeText());
    }

    private bool HasAccessibleAlternativeText()
    {
        return this.AlternativeTexts.Any(static text => !string.IsNullOrWhiteSpace(text.LanguageCode)
            && !string.IsNullOrWhiteSpace(text.Value));
    }

    private static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }
}
