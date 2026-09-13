using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageCopyCatalog
{
    private static readonly IReadOnlyDictionary<string, ShareSocialImageLocalizedCopy> Copies =
        new Dictionary<string, ShareSocialImageLocalizedCopy>(StringComparer.Ordinal)
        {
            ["de"] = Create(
                "de-DE",
                "MEIN BESUCHSRÜCKBLICK",
                "MEIN JAHRESRÜCKBLICK",
                "MEIN PARKPASS",
                "Persönliche Auswahl",
                "Ein Parkfan",
                "Freizeitpark",
                "Mein Highlight",
                "PARKS", "BESUCHE", "FAHRTEN", "ATTRAKTIONEN", "BEWERTUNG"),
            ["en"] = Create(
                "en-GB",
                "MY VISIT RECAP",
                "MY YEAR IN PARKS",
                "MY PARK PASSPORT",
                "Personal selection",
                "A park fan",
                "Amusement park",
                "My highlight",
                "PARKS", "VISITS", "RIDES", "ATTRACTIONS", "RATING"),
            ["es"] = Create(
                "es-ES",
                "MI RESUMEN DE VISITA",
                "MI AÑO EN PARQUES",
                "MI PASAPORTE DE PARQUES",
                "Selección personal",
                "Una persona aficionada",
                "Parque de atracciones",
                "Mi momento destacado",
                "PARQUES", "VISITAS", "VUELTAS", "ATRACCIONES", "NOTA"),
            ["fr"] = Create(
                "fr-FR",
                "MON RÉCAP DE VISITE",
                "MON ANNÉE DANS LES PARCS",
                "MON PASSEPORT DE PARCS",
                "Sélection personnelle",
                "Un fan de parcs",
                "Parc d’attractions",
                "Mon temps fort",
                "PARCS", "VISITES", "TOURS", "ATTRACTIONS", "NOTE"),
            ["it"] = Create(
                "it-IT",
                "IL MIO RIEPILOGO DELLA VISITA",
                "IL MIO ANNO NEI PARCHI",
                "IL MIO PASSAPORTO DEI PARCHI",
                "Selezione personale",
                "Un appassionato di parchi",
                "Parco divertimenti",
                "Il mio momento migliore",
                "PARCHI", "VISITE", "GIRI", "ATTRAZIONI", "VOTO"),
            ["nl"] = Create(
                "nl-NL",
                "MIJN BEZOEK IN HET KORT",
                "MIJN JAAR IN PRETPARKEN",
                "MIJN PARKPASPOORT",
                "Persoonlijke selectie",
                "Een pretparkfan",
                "Pretpark",
                "Mijn hoogtepunt",
                "PARKEN", "BEZOEKEN", "RITTEN", "ATTRACTIES", "SCORE"),
            ["pl"] = Create(
                "pl-PL",
                "PODSUMOWANIE MOJEJ WIZYTY",
                "MÓJ ROK W PARKACH",
                "MÓJ PASZPORT PARKÓW",
                "Osobisty wybór",
                "Miłośnik parków",
                "Park rozrywki",
                "Moja atrakcja dnia",
                "PARKI", "WIZYTY", "PRZEJAZDY", "ATRAKCJE", "OCENA"),
            ["pt"] = Create(
                "pt-PT",
                "O RESUMO DA MINHA VISITA",
                "O MEU ANO NOS PARQUES",
                "O MEU PASSAPORTE DE PARQUES",
                "Seleção pessoal",
                "Uma pessoa fã de parques",
                "Parque de diversões",
                "O meu destaque",
                "PARQUES", "VISITAS", "VOLTAS", "ATRAÇÕES", "NOTA"),
        };

    public static ShareSocialImageLocalizedCopy Resolve(string language)
    {
        return Copies.TryGetValue(language, out ShareSocialImageLocalizedCopy? copy)
            ? copy
            : Copies["en"];
    }

    private static ShareSocialImageLocalizedCopy Create(
        string culture,
        string visitTitle,
        string yearTitle,
        string passportTitle,
        string personalLabel,
        string anonymousSubject,
        string unknownPark,
        string highlightLabel,
        string parks,
        string visits,
        string rides,
        string attractions,
        string rating)
    {
        return new ShareSocialImageLocalizedCopy(
            culture,
            visitTitle,
            yearTitle,
            passportTitle,
            personalLabel,
            anonymousSubject,
            unknownPark,
            highlightLabel,
            new Dictionary<ShareSocialImageMetricKind, string>
            {
                [ShareSocialImageMetricKind.Parks] = parks,
                [ShareSocialImageMetricKind.Visits] = visits,
                [ShareSocialImageMetricKind.Rides] = rides,
                [ShareSocialImageMetricKind.Attractions] = attractions,
                [ShareSocialImageMetricKind.Rating] = rating,
            });
    }
}
