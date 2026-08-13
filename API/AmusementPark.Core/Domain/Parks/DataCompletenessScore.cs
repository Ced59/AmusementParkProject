using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using System.Net;
using System.Text.RegularExpressions;

namespace AmusementPark.Core.Domain.Parks;

public enum DataQualityLevel
{
    Critical = 0,
    Weak = 1,
    Partial = 2,
    Publishable = 3,
    Good = 4,
    Excellent = 5,
}

public sealed record DataCompletenessScore
{
    private const int PublicationBlockerScoreCeiling = 95;

    public int CompletenessScore { get; init; }

    public DataQualityLevel DataQualityLevel { get; init; }

    public int ApplicableMaxPoints { get; init; }

    public int EarnedPoints { get; init; }

    public string? PublicationBlocker { get; init; }

    public static DataCompletenessScore FromPoints(
        int earnedPoints,
        int applicableMaxPoints,
        string? publicationBlocker = null)
    {
        int normalizedApplicableMaxPoints = Math.Max(0, applicableMaxPoints);
        int normalizedEarnedPoints = Math.Clamp(earnedPoints, 0, normalizedApplicableMaxPoints);
        string? normalizedPublicationBlocker = string.IsNullOrWhiteSpace(publicationBlocker)
            ? null
            : publicationBlocker.Trim();
        int completenessScore = normalizedApplicableMaxPoints == 0
            ? 0
            : (int)Math.Round((double)normalizedEarnedPoints / normalizedApplicableMaxPoints * 100d, MidpointRounding.AwayFromZero);

        completenessScore = Math.Clamp(completenessScore, 0, 100);
        if (normalizedPublicationBlocker is not null)
        {
            completenessScore = Math.Min(completenessScore, PublicationBlockerScoreCeiling);
        }

        return new DataCompletenessScore
        {
            CompletenessScore = completenessScore,
            DataQualityLevel = ResolveLevel(completenessScore),
            ApplicableMaxPoints = normalizedApplicableMaxPoints,
            EarnedPoints = normalizedEarnedPoints,
            PublicationBlocker = normalizedPublicationBlocker,
        };
    }

    private static DataQualityLevel ResolveLevel(int completenessScore)
    {
        if (completenessScore >= 95)
        {
            return DataQualityLevel.Excellent;
        }

        if (completenessScore >= 85)
        {
            return DataQualityLevel.Good;
        }

        if (completenessScore >= 70)
        {
            return DataQualityLevel.Publishable;
        }

        if (completenessScore >= 50)
        {
            return DataQualityLevel.Partial;
        }

        if (completenessScore >= 30)
        {
            return DataQualityLevel.Weak;
        }

        return DataQualityLevel.Critical;
    }
}

public sealed record ParkDataCompletenessContext
{
    public bool ProjectForPublication { get; init; }

    public int ParkItemsTotalCount { get; init; }

    public int ParkItemsVisibleCount { get; init; }

    public int DistinctParkItemCategoryCount { get; init; }

    public int ClosedImportantParkItemsCount { get; init; }

    public int ParkItemsWithKnownStatusOrDatesCount { get; init; }

    public int AttractionManufacturerIdsCount { get; init; }

    public int AttractionsWithAccessConditionsCount { get; init; }

    public bool HasOfficialZones { get; init; }

    public int ZonesTotalCount { get; init; }

    public int ZonesWithDescriptionsCount { get; init; }

    public int ParkItemsAttachedToZonesCount { get; init; }

    public int ParkItemsWithDescriptionsCount { get; init; }

    public int CommercialOrServiceItemsWithDescriptionsCount { get; init; }

    public int ParkPublishedImageCount { get; init; }

    public int ParkImagesWithResolvedOwnerCount { get; init; }

    public int ParkImagesWithLocalizedAltTextCount { get; init; }

    public int ParkItemPublishedImageCount { get; init; }

    public bool HasOriginalMedia { get; init; }

    public bool HasOpeningHours { get; init; }

    public ParkOpeningHoursAdminStatus OpeningHoursStatus { get; init; } = ParkOpeningHoursAdminStatus.NotConfigured;

    public bool HasOpeningHoursSource { get; init; }

    public bool HasOpeningHoursTimeZone { get; init; }

    public bool HasOpeningHoursExceptions { get; init; }

    public bool HasOpeningHoursRecentVerification { get; init; }

    public int ParkHistoryEventCount { get; init; }

    public int MajorHistoryEventCount { get; init; }

    public int ParkItemHistoryEventCount { get; init; }

    public int PublishedArticleCount { get; init; }

    public int StructuredArticleCount { get; init; }

    public int LocalizedHistoryContentCount { get; init; }

    public int HistoryEventsWithSourcesCount { get; init; }

    public int HistoryEventsWithMediaCount { get; init; }

    public int ImportantReferencesWithDescriptionsCount { get; init; }

    public int ReferencesWithUsefulDetailsCount { get; init; }

    public bool HasNoProbableDuplicate { get; init; } = true;

    public bool HasCleanLegacyDataOrDocumentedDebt { get; init; } = true;

    public bool HasResolvedAttachmentKeys { get; init; } = true;

    public bool HasNoKnownBlockingWarnings { get; init; } = true;

    public bool HasCriticalSources { get; init; }

    public bool HasNoInventedDates { get; init; } = true;

    public bool HasStructuredTechnicalDataOnly { get; init; } = true;

    public bool HasNoForbiddenPublicText { get; init; } = true;

    public bool HasDocumentedRemainingDebt { get; init; }

    public bool HasPublicSeoSignals { get; init; }
}

public sealed record ParkItemDataCompletenessContext
{
    public bool ParentParkResolved { get; init; } = true;

    public bool ParentParkVisible { get; init; }

    public bool HasNoDuplicateInPark { get; init; } = true;

    public bool HasOfficialZoneContext { get; init; }

    public bool HasUsefulVisitGrouping { get; init; }

    public bool HasRepresentativeImage { get; init; }

    public bool HasResolvedImageOwner { get; init; }

    public bool HasLocalizedImageAltText { get; init; }

    public bool HasNonMisleadingImage { get; init; }

    public bool HasOriginalMedia { get; init; }

    public bool HasHistoricalImageContext { get; init; }

    public int HistoryEventCount { get; init; }

    public int ClosureOrChangeHistoryEventCount { get; init; }

    public bool HasTimelineConsistentWithParent { get; init; } = true;

    public int PublishedArticleCount { get; init; }

    public int HistoryEventsWithSourcesCount { get; init; }

    public bool HasReferenceDetailsOrDocumentedDebt { get; init; }

    public bool HasInternalLinks { get; init; }

    public bool HasNoDuplicateReferences { get; init; } = true;

    public bool HasSeoSignals { get; init; }

    public bool HasNoPlaceholderPublicPage { get; init; } = true;

    public bool HasStructuredDataSignals { get; init; }

    public bool HasHumanReviewOrDocumentedDebt { get; init; }

    public bool HasNoUnresolvedReferences { get; init; } = true;

    public bool HasNoForbiddenPublicText { get; init; } = true;
}

internal sealed class DataCompletenessScoreBuilder
{
    private int earnedPoints;
    private int applicableMaxPoints;
    private string? publicationBlocker;

    public bool HasMissingPoints => this.earnedPoints < this.applicableMaxPoints;

    public void Add(bool isSatisfied, int points)
    {
        if (points <= 0)
        {
            return;
        }

        this.applicableMaxPoints += points;
        if (isSatisfied)
        {
            this.earnedPoints += points;
        }
    }

    public void AddPartial(int earnedParts, int applicableParts, int points)
    {
        if (points <= 0 || applicableParts <= 0)
        {
            return;
        }

        this.applicableMaxPoints += points;
        int normalizedEarnedParts = Math.Clamp(earnedParts, 0, applicableParts);
        this.earnedPoints += (int)Math.Round((double)normalizedEarnedParts / applicableParts * points, MidpointRounding.AwayFromZero);
    }

    public void AddIfApplicable(bool isApplicable, bool isSatisfied, int points)
    {
        if (!isApplicable)
        {
            return;
        }

        this.Add(isSatisfied, points);
    }

    public void AddPublicationBlocker(bool isBlocking, string blockerKey)
    {
        if (!isBlocking || string.IsNullOrWhiteSpace(blockerKey))
        {
            return;
        }

        string normalizedBlockerKey = blockerKey.Trim();
        if (this.publicationBlocker is null
            || string.CompareOrdinal(normalizedBlockerKey, this.publicationBlocker) < 0)
        {
            this.publicationBlocker = normalizedBlockerKey;
        }
    }

    public DataCompletenessScore Build()
    {
        return DataCompletenessScore.FromPoints(this.earnedPoints, this.applicableMaxPoints, this.publicationBlocker);
    }
}

public static class DataCompletenessScoringRules
{
    public const string ForbiddenPublicTextPublicationBlocker = "public-text.forbidden-editorial-language";

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

    private static readonly Regex WhitespaceRegex = new Regex(
        @"\s+",
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
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalizedValue = NormalizePublicText(value);
        if (InternalJargonRegex.IsMatch(normalizedValue)
            || ForbiddenEditorialPhrases.Any(phrase => normalizedValue.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            || RawTechnicalMetricRegex.IsMatch(normalizedValue))
        {
            return true;
        }

        int technicalNarrativeCategoryCount = TechnicalNarrativeCategoryRegexes.Count(regex => regex.IsMatch(normalizedValue));
        return technicalNarrativeCategoryCount >= 3;
    }

    private static string NormalizePublicText(string value)
    {
        string decodedValue = WebUtility.HtmlDecode(value);
        string withoutHtml = HtmlTagRegex.Replace(decodedValue, " ");
        return WhitespaceRegex.Replace(withoutHtml, " ").Trim();
    }
}
