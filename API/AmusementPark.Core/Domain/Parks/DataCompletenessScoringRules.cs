using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AmusementPark.Core.Domain.Parks;

public static class DataCompletenessScoringRules
{
    public const string ForbiddenPublicTextPublicationBlocker = "public-text.forbidden-editorial-language";

    public const string FormulaicPublicTextPublicationBlocker = "public-text.formulaic-content";

    public const string MissingRequiredLogoPublicationBlocker = "media.logo-required";

    private static readonly string[] PublicLanguageCodes =
    [
        "fr",
        "en",
        "de",
        "nl",
        "it",
        "es",
        "pl",
        "pt",
    ];

    private static readonly string[] ForbiddenEditorialPhrases =
    [
        "public page",
        "page publique",
        "página pública",
        "öffentliche betreiberseite",
        "pagina pubblica",
        "publiczna strona",
        "openbare pagina",
        "independent visit",
        "visite indépendante",
        "visita independiente",
        "unabhängiger besuch",
        "visita indipendente",
        "niezależna wizyta",
        "onafhankelijk bezoek",
        "visita independente",
        "current inventory",
        "inventaire actuel",
        "inventario actual",
        "heutige sammlung",
        "proposte attuali",
        "obecna kolekcja",
        "huidige verzameling",
        "these sources",
        "ces sources",
        "ambas fuentes",
        "zusammen zeichnen die quellen",
        "le fonti descrivono",
        "źródła przedstawiają",
        "samen schetsen de bronnen",
        "as fontes descrevem",
        "public mapping",
        "cartographie publique",
        "visitor content",
        "contenus visiteurs",
        "presence confirmed",
        "présence publique confirmée",
        "repère documentaire",
        "documentary marker",
        "for a visitor page",
        "pour une fiche visiteur",
        "contenu public",
        "public content",
        "élément de parc",
        "park item",
        "dans la base",
        "in the database",
        "ce que ça apporte à la journée",
        "what it adds to the day",
        "comment l’intégrer dans la journée",
        "comment l'integrer dans la journee",
        "how to fit it into the day",
        "une pause entre les files",
        "a break between queues",
        "une expérience utile pour varier le parcours",
        "a useful experience to vary the itinerary",
        "une place claire dans la visite",
        "a clear place in the visit",
    ];

    private static readonly Regex InternalJargonRegex = new Regex(
        @"\b(?:audit|admin|upsert|seo|m14|to\s+review|completeness\s+score|score\s+de\s+complétude)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex HtmlTagRegex = new Regex(
        "<[^>]+>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex EncodedTextEntityRegex = new Regex(
        @"&(?:(?!(?:amp|lt|gt);)(?:#[0-9]+|#x[0-9a-f]+|[a-z][a-z0-9]+);|amp;(?:#[0-9]+|#x[0-9a-f]+|[a-z][a-z0-9]+);)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex HtmlEntityRegex = new Regex(
        @"&(?:#[0-9]+|#x[0-9a-f]+|[a-z][a-z0-9]+);",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex WhitespaceRegex = new Regex(
        @"\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex HtmlBlockBoundaryRegex = new Regex(
        @"</?(?:h[1-6]|p|div|li|section|article|blockquote|br)\b[^>]*>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex SentenceBoundaryRegex = new Regex(
        @"(?<=[.!?])\s+|[\r\n]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex[] SingleOccurrenceFormulaRegexes =
    [
        new Regex(@"\b(?:appartient\s+à\s+l['’]+univers\s+de|belongs\s+to\s+the\s+world\s+of|gehört\s+zur\s+welt\s+von|behoort\s+tot\s+de\s+wereld\s+van|appartiene\s+al\s+mondo\s+di|forma\s+parte\s+del\s+universo\s+de|należy\s+do\s+świata|pertence\s+ao\s+universo\s+de)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
        new Regex(@"\b(?:prolonge\s+l['’]+atmosphère\s+de|extends\s+the\s+atmosphere\s+of|führt\s+die\s+atmosphäre\s+von|zet\s+de\s+sfeer\s+van|prolunga\s+l['’]+atmosfera\s+di|prolonga\s+la\s+atmósfera\s+de|rozwija\s+atmosferę|prolonga\s+o\s+ambiente\s+de)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
        new Regex(@"^(?:(?:une\s+)?scène\s+de\s+.+\s+à|a\s+scene\s+from\s+.+\s+at|eine\s+szene\s+aus\s+.+\s+im|een\s+scène\s+van\s+.+\s+in|una\s+scena\s+di\s+.+\s+a|una\s+escena\s+de\s+.+\s+en|scena\s+z\s+.+\s+w|uma\s+cena\s+de\s+.+\s+na)\s+disneyland\s+park[.!]?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
    ];

    private static readonly Regex WordRegex = new Regex(
        @"[\p{L}\p{N}]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex RawTechnicalMetricRegex = new Regex(
        @"\b\d+(?:[.,]\d+)?\s*(?:km\s*/?\s*h|mph|m\s*/\s*s|m|cm|met(?:er|re)s?|mètres?|feet|foot|ft|seconds?|secondes?|sekunden?|seconden?|secondi|segundos?|sekund(?:a|y)?|minutes?|minuten?|minuti|minutos?|riders?\s*(?:per|/)\s*hour|passagers?\s*(?:par|/)\s*heure|personas?\s*(?:por|/)\s*hora|personen?\s*(?:pro|per|/)\s*stunde)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex[] TechnicalNarrativeCategoryRegexes =
    [
        new Regex(@"\b(?:track|rails?|layout|structure|tracé|voies?|schienen?|strecke|konstruktion|tracciato|rotaie?|struttura|trazado|vías?|estructura|tory?|szyny?|konstrukcja|baan|rails?|constructie|percurso|trilhos?|estrutura)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
        new Regex(@"\b(?:vehicles?|seats?|restraints?|harness|véhicules?|sièges?|retenues?|harnais|fahrzeuge?|sitze?|rückhaltesystem|veicoli?|sedili?|ritenute|vehículos?|asientos?|sujeciones?|pojazdy?|siedzenia?|zabezpieczenia|voertuigen?|zitplaatsen?|beugels?|veículos?|assentos?|retenções?)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
        new Regex(@"\b(?:rotations?|accelerations?|accélérations?|inversions?|launch|propulsion|lift\s+hill|chain\s+lift|rotazioni?|accelerazioni?|lancio|rotaciones?|aceleraciones?|lanzamiento|rotationen?|beschleunigungen?|abschuss|obroty?|przyspieszeni|wystrzelen|rotaties?|versnellingen?|lancering|rotações?|acelerações?|lançamento)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
        new Regex(@"\b(?:speed|duration|capacity|vitesse|durée|capacité|geschwindigkeit|dauer|kapazität|velocità|durata|capacità|velocidad|duración|capacidad|prędkość|czas\s+trwania|pojemność|snelheid|duur|capaciteit|velocidade|duração|capacidade)\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)),
    ];

    public static bool HasText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool HasMeaningfulText(string? value, int minimumLength = 40)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= minimumLength;
    }

    public static bool HasAnyLocalizedText(IEnumerable<LocalizedText> values)
    {
        return values.Any(static value => HasText(value.Value));
    }

    public static int CountPublicLanguagesWithText(IEnumerable<LocalizedText> values)
    {
        HashSet<string> expectedLanguageCodes = PublicLanguageCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return values
            .Where(static value => HasText(value.LanguageCode) && HasText(value.Value))
            .Select(static value => value.LanguageCode.Trim())
            .Where(languageCode => expectedLanguageCodes.Contains(languageCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    public static int PublicLanguageCount => PublicLanguageCodes.Length;

    public static bool HasValidPosition(GeoPoint? position)
    {
        return position is not null
            && (Math.Abs(position.Latitude) > double.Epsilon || Math.Abs(position.Longitude) > double.Epsilon);
    }

    public static bool IsPlaceholderName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        string normalizedValue = value.Trim();
        return string.Equals(normalizedValue, "todo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, "tbd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, "unknown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, "unknown park", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValue, "unknown item", StringComparison.OrdinalIgnoreCase)
            || normalizedValue.StartsWith("new park", StringComparison.OrdinalIgnoreCase)
            || normalizedValue.StartsWith("new item", StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasInternalJargon(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return InternalJargonRegex.IsMatch(NormalizePublicText(value));
    }

    public static bool HasForbiddenPublicText(string? value)
    {
        return HasForbiddenRichPublicText(value);
    }

    public static bool HasForbiddenRichPublicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (HasEncodedDisplayEntity(value))
        {
            return true;
        }

        return HasForbiddenEditorialPublicText(value, rejectTechnicalMetrics: true);
    }

    public static bool HasForbiddenPlainPublicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (HasHtmlEntity(value))
        {
            return true;
        }

        return HasForbiddenEditorialPublicText(value, rejectTechnicalMetrics: true);
    }

    public static bool HasForbiddenStructuredLabelPublicText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (HasHtmlEntity(value))
        {
            return true;
        }

        return HasForbiddenEditorialPublicText(value, rejectTechnicalMetrics: false);
    }

    private static bool HasForbiddenEditorialPublicText(string value, bool rejectTechnicalMetrics)
    {

        string normalizedValue = NormalizePublicText(value);
        if (InternalJargonRegex.IsMatch(normalizedValue)
            || ForbiddenEditorialPhrases.Any(phrase => normalizedValue.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            || (rejectTechnicalMetrics && RawTechnicalMetricRegex.IsMatch(normalizedValue)))
        {
            return true;
        }

        int technicalNarrativeCategoryCount = TechnicalNarrativeCategoryRegexes.Count(regex => regex.IsMatch(normalizedValue));
        return technicalNarrativeCategoryCount >= 3;
    }

    public static bool HasEncodedDisplayEntity(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && EncodedTextEntityRegex.IsMatch(value);
    }

    public static bool HasHtmlEntity(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && HtmlEntityRegex.IsMatch(value);
    }

    public static bool HasFormulaicPublicText(
        IEnumerable<LocalizedText> publicTexts,
        IEnumerable<string?> entityNames)
    {
        return FindFormulaicPublicTextIssue(publicTexts, entityNames) is not null;
    }

    public static FormulaicPublicTextIssue? FindFormulaicPublicTextIssue(
        IEnumerable<LocalizedText> publicTexts,
        IEnumerable<string?> entityNames)
    {
        ArgumentNullException.ThrowIfNull(publicTexts);
        ArgumentNullException.ThrowIfNull(entityNames);

        List<(int Index, LocalizedText Text)> populatedPublicTexts = publicTexts
            .Where(static text => !string.IsNullOrWhiteSpace(text.Value))
            .Select(static (text, index) => (Index: index + 1, Text: text))
            .ToList();
        foreach ((int Index, LocalizedText Text) indexedPublicText in populatedPublicTexts)
        {
            LocalizedText publicText = indexedPublicText.Text;
            string normalizedPublicText = NormalizePublicText(publicText.Value!);
            foreach (Regex formulaRegex in SingleOccurrenceFormulaRegexes)
            {
                Match match = formulaRegex.Match(normalizedPublicText);
                if (!match.Success)
                {
                    continue;
                }

                return new FormulaicPublicTextIssue
                {
                    MatchType = "single-formula",
                    LanguageCode = NormalizeLanguageCode(publicText.LanguageCode),
                    FirstDocumentIndex = indexedPublicText.Index,
                    SecondDocumentIndex = indexedPublicText.Index,
                    FirstDocumentSha256 = ComputeSha256(publicText.Value!),
                    SecondDocumentSha256 = ComputeSha256(publicText.Value!),
                    FingerprintSha256 = ComputeSha256(match.Value),
                    FirstDocumentPreview = CreateDiagnosticPreview(publicText.Value!),
                    SecondDocumentPreview = CreateDiagnosticPreview(publicText.Value!),
                    FingerprintPreview = CreateDiagnosticPreview(match.Value),
                };
            }
        }

        List<string> normalizedEntityNames = entityNames
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => NormalizePublicText(name!).ToLowerInvariant())
            .Where(static name => name.Length >= 3)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static name => name.Length)
            .ToList();
        Dictionary<string, int> firstDocumentBySentence = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, int> firstDocumentByLongSequence = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (IGrouping<string, (int Index, LocalizedText Text)> languageGroup in populatedPublicTexts
            .GroupBy(
                static indexedText => NormalizeLanguageCode(indexedText.Text.LanguageCode),
                StringComparer.OrdinalIgnoreCase))
        {
            firstDocumentBySentence.Clear();
            firstDocumentByLongSequence.Clear();

            foreach ((int Index, LocalizedText Text) indexedPublicText in languageGroup)
            {
                LocalizedText publicText = indexedPublicText.Text;
                string decodedValue = WebUtility.HtmlDecode(publicText.Value ?? string.Empty);
                string withBoundaries = HtmlBlockBoundaryRegex.Replace(decodedValue, ". ");
                string withoutHtml = HtmlTagRegex.Replace(withBoundaries, " ");

                foreach (string rawSentence in SentenceBoundaryRegex.Split(withoutHtml))
                {
                    string normalizedSentence = WhitespaceRegex.Replace(rawSentence, " ").Trim().ToLowerInvariant();
                    foreach (string entityName in normalizedEntityNames)
                    {
                        normalizedSentence = normalizedSentence.Replace(entityName, " ", StringComparison.Ordinal);
                    }

                    normalizedSentence = WhitespaceRegex.Replace(normalizedSentence, " ").Trim(' ', '.', ',', ';', ':', '-', '–', '—');
                    List<string> words = WordRegex.Matches(normalizedSentence)
                        .Select(static match => match.Value)
                        .ToList();
                    if (words.Count < 6)
                    {
                        continue;
                    }

                    string sentenceFingerprint = string.Join(' ', words);
                    int? firstSentenceDocumentIndex = FindFirstDocumentIndex(
                        firstDocumentBySentence,
                        sentenceFingerprint,
                        indexedPublicText.Index);
                    if (firstSentenceDocumentIndex.HasValue)
                    {
                        return new FormulaicPublicTextIssue
                        {
                            MatchType = "sentence",
                            LanguageCode = NormalizeLanguageCode(publicText.LanguageCode),
                            FirstDocumentIndex = firstSentenceDocumentIndex.Value,
                            SecondDocumentIndex = indexedPublicText.Index,
                            FirstDocumentSha256 = ComputeDocumentSha256(populatedPublicTexts, firstSentenceDocumentIndex.Value),
                            SecondDocumentSha256 = ComputeSha256(publicText.Value!),
                            FingerprintSha256 = ComputeSha256(sentenceFingerprint),
                            FirstDocumentPreview = CreateDocumentPreview(populatedPublicTexts, firstSentenceDocumentIndex.Value),
                            SecondDocumentPreview = CreateDiagnosticPreview(publicText.Value!),
                            FingerprintPreview = CreateDiagnosticPreview(sentenceFingerprint),
                        };
                    }

                    const int longSequenceWordCount = 9;
                    for (int index = 0; index <= words.Count - longSequenceWordCount; index += 1)
                    {
                        string sequenceFingerprint = string.Join(' ', words.Skip(index).Take(longSequenceWordCount));
                        int? firstSequenceDocumentIndex = FindFirstDocumentIndex(
                            firstDocumentByLongSequence,
                            sequenceFingerprint,
                            indexedPublicText.Index);
                        if (firstSequenceDocumentIndex.HasValue)
                        {
                            return new FormulaicPublicTextIssue
                            {
                                MatchType = "long-sequence",
                                LanguageCode = NormalizeLanguageCode(publicText.LanguageCode),
                                FirstDocumentIndex = firstSequenceDocumentIndex.Value,
                                SecondDocumentIndex = indexedPublicText.Index,
                                FirstDocumentSha256 = ComputeDocumentSha256(populatedPublicTexts, firstSequenceDocumentIndex.Value),
                                SecondDocumentSha256 = ComputeSha256(publicText.Value!),
                                FingerprintSha256 = ComputeSha256(sequenceFingerprint),
                                FirstDocumentPreview = CreateDocumentPreview(populatedPublicTexts, firstSequenceDocumentIndex.Value),
                                SecondDocumentPreview = CreateDiagnosticPreview(publicText.Value!),
                                FingerprintPreview = CreateDiagnosticPreview(sequenceFingerprint),
                            };
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string ComputeDocumentSha256(
        IReadOnlyList<(int Index, LocalizedText Text)> publicTexts,
        int documentIndex)
    {
        return ComputeSha256(publicTexts[documentIndex - 1].Text.Value!);
    }

    private static string CreateDocumentPreview(
        IReadOnlyList<(int Index, LocalizedText Text)> publicTexts,
        int documentIndex)
    {
        return CreateDiagnosticPreview(publicTexts[documentIndex - 1].Text.Value!);
    }

    private static string CreateDiagnosticPreview(string value)
    {
        const int maximumLength = 240;
        string normalizedValue = NormalizePublicText(value);
        return normalizedValue.Length <= maximumLength
            ? normalizedValue
            : normalizedValue[..maximumLength] + "…";
    }

    private static string ComputeSha256(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static int? FindFirstDocumentIndex(
        IDictionary<string, int> firstDocumentByFingerprint,
        string fingerprint,
        int documentIndex)
    {
        if (firstDocumentByFingerprint.TryGetValue(fingerprint, out int firstDocumentIndex))
        {
            return firstDocumentIndex == documentIndex ? null : firstDocumentIndex;
        }

        firstDocumentByFingerprint[fingerprint] = documentIndex;
        return null;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? "und" : languageCode.Trim();
    }

    private static string NormalizePublicText(string value)
    {
        string decodedValue = WebUtility.HtmlDecode(value);
        string withoutHtml = HtmlTagRegex.Replace(decodedValue, " ");
        return WhitespaceRegex.Replace(withoutHtml, " ").Trim();
    }
}
