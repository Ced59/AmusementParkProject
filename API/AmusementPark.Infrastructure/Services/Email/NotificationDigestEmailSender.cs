using System.Globalization;
using System.Text;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class NotificationDigestEmailSender : INotificationDigestEmailSender
{
    private const int MaximumDisplayedEntries = 12;
    private readonly IEmailSender emailSender;
    private readonly IUserAuthenticationSettings authenticationSettings;
    private readonly BrandedEmailTemplateRenderer templateRenderer;

    public NotificationDigestEmailSender(
        IEmailSender emailSender,
        IUserAuthenticationSettings authenticationSettings,
        BrandedEmailTemplateRenderer templateRenderer)
    {
        this.emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        this.authenticationSettings = authenticationSettings
            ?? throw new ArgumentNullException(nameof(authenticationSettings));
        this.templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
    }

    public Task SendAsync(
        NotificationDigestEmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        string language = NormalizeLanguage(message.Language);
        string baseUrl = this.authenticationSettings.FrontendBaseUrl.TrimEnd('/');
        string preferencesUrl = $"{baseUrl}/{language}/profile/notifications";
        string unsubscribeUrl = $"{baseUrl}/api/notification-email/unsubscribe?token="
            + Uri.EscapeDataString(message.UnsubscribeToken);
        string title = ResolveTitle(language, message.Frequency);
        BrandedEmailMetric[] metrics = message.Entries
            .Take(MaximumDisplayedEntries)
            .Select(entry => new BrandedEmailMetric(
                ResolveTargetLabel(language, entry),
                ResolveEntrySummary(language, entry)))
            .ToArray();
        int hiddenCount = Math.Max(0, message.Entries.Count - metrics.Length);
        List<string> paragraphs = new List<string>
        {
            ResolveIntro(language, message.Entries.Count),
            ResolveProof(language),
        };
        if (hiddenCount > 0)
        {
            paragraphs.Add(ResolveHiddenCount(language, hiddenCount));
        }

        string htmlBody = this.templateRenderer.Render(new BrandedEmailTemplateModel
        {
            Language = language,
            Preheader = ResolvePreheader(language, message.Entries.Count),
            Badge = ResolveBadge(language),
            Title = title,
            Paragraphs = paragraphs,
            Metrics = metrics,
            Highlight = new BrandedEmailHighlight(
                ResolvePeriodLabel(language),
                FormatPeriod(message.PeriodStartUtc, message.PeriodEndUtc, language)),
            Action = new BrandedEmailAction(
                ResolveManageAction(language),
                preferencesUrl),
            FooterNote = ResolveFooter(language),
        });
        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["List-Unsubscribe"] = $"<{unsubscribeUrl}>",
            ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
        };
        EmailMessage email = new EmailMessage(
            message.RecipientEmail,
            ResolveSubject(language, message.Frequency),
            htmlBody,
            BuildTextBody(language, message, preferencesUrl),
            headers);
        return this.emailSender.SendAsync(email, cancellationToken);
    }

    private static string BuildTextBody(
        string language,
        NotificationDigestEmailMessage message,
        string preferencesUrl)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(ResolveTitle(language, message.Frequency));
        builder.AppendLine();
        builder.AppendLine(ResolveIntro(language, message.Entries.Count));
        builder.AppendLine(ResolveProof(language));
        builder.AppendLine();
        foreach (NotificationDigestEmailEntry entry in message.Entries.Take(MaximumDisplayedEntries))
        {
            builder.Append("- ")
                .Append(ResolveTargetLabel(language, entry))
                .Append(": ")
                .AppendLine(ResolveEntrySummary(language, entry));
        }

        int hiddenCount = Math.Max(0, message.Entries.Count - MaximumDisplayedEntries);
        if (hiddenCount > 0)
        {
            builder.AppendLine(ResolveHiddenCount(language, hiddenCount));
        }

        builder.AppendLine();
        builder.AppendLine(ResolveManageAction(language));
        builder.AppendLine(preferencesUrl);
        builder.AppendLine();
        builder.AppendLine(ResolveFooter(language));
        return builder.ToString().Trim();
    }

    private static string ResolveTargetLabel(string language, NotificationDigestEmailEntry entry)
    {
        string fallback = language switch
        {
            "de" => "Beobachteter Ort",
            "es" => "Lugar seguido",
            "fr" => "Lieu suivi",
            "it" => "Luogo seguito",
            "nl" => "Gevolgde plek",
            "pl" => "Obserwowane miejsce",
            "pt" => "Local seguido",
            _ => "Watched place",
        };
        string name = string.IsNullOrWhiteSpace(entry.TargetName) ? fallback : entry.TargetName;
        return string.IsNullOrWhiteSpace(entry.ParentParkName)
            ? name
            : $"{entry.ParentParkName} · {name}";
    }

    private static string ResolveEntrySummary(string language, NotificationDigestEmailEntry entry)
    {
        string change = ResolveEventCategory(language, entry.EventType);
        string status = entry.Status switch
        {
            FactualChangeStatus.Retracted => ResolveRetraction(language),
            FactualChangeStatus.Corrected => ResolveCorrection(language),
            _ => ResolveVerified(language),
        };
        string source = string.IsNullOrWhiteSpace(entry.SourceLabel)
            ? ResolveVerifiedSource(language)
            : entry.SourceLabel;
        return $"{change} · {status} · {source}";
    }

    private static string ResolveEventCategory(string language, FactualEventType eventType)
    {
        int category = eventType switch
        {
            FactualEventType.OpeningCalendarPublished
                or FactualEventType.OpeningCalendarChanged
                or FactualEventType.SeasonOpeningConfirmed
                or FactualEventType.SeasonClosingConfirmed => 1,
            FactualEventType.OpeningDateConfirmed
                or FactualEventType.OpeningDateChanged
                or FactualEventType.AttractionAnnouncedOfficially => 2,
            FactualEventType.ParkTemporaryClosureConfirmed
                or FactualEventType.ParkPermanentClosureConfirmed
                or FactualEventType.ParkReopeningConfirmed
                or FactualEventType.TemporarilyClosedConfirmed
                or FactualEventType.ReopenedConfirmed
                or FactualEventType.PermanentClosureConfirmed
                or FactualEventType.OpenedConfirmed => 3,
            FactualEventType.TicketPricePublishedOrChanged => 4,
            FactualEventType.ParkNameChanged
                or FactualEventType.Renamed
                or FactualEventType.OperatorChanged => 5,
            FactualEventType.MajorRestrictionChanged
                or FactualEventType.LocationOrCategoryCorrected => 6,
            _ => 7,
        };
        return (language, category) switch
        {
            ("de", 1) => "Öffnungszeiten aktualisiert",
            ("de", 2) => "Eröffnung aktualisiert",
            ("de", 3) => "Betriebsstatus aktualisiert",
            ("de", 4) => "Preis aktualisiert",
            ("de", 5) => "Identität aktualisiert",
            ("de", 6) => "Praktische Angaben aktualisiert",
            ("de", _) => "Verifizierte Information aktualisiert",
            ("es", 1) => "Calendario actualizado",
            ("es", 2) => "Apertura actualizada",
            ("es", 3) => "Estado de actividad actualizado",
            ("es", 4) => "Precio actualizado",
            ("es", 5) => "Identidad actualizada",
            ("es", 6) => "Información práctica actualizada",
            ("es", _) => "Información verificada actualizada",
            ("fr", 1) => "Calendrier d’ouverture mis à jour",
            ("fr", 2) => "Ouverture mise à jour",
            ("fr", 3) => "État d’activité mis à jour",
            ("fr", 4) => "Tarif mis à jour",
            ("fr", 5) => "Identité mise à jour",
            ("fr", 6) => "Information pratique mise à jour",
            ("fr", _) => "Information vérifiée mise à jour",
            ("it", 1) => "Calendario aggiornato",
            ("it", 2) => "Apertura aggiornata",
            ("it", 3) => "Stato di attività aggiornato",
            ("it", 4) => "Prezzo aggiornato",
            ("it", 5) => "Identità aggiornata",
            ("it", 6) => "Informazione pratica aggiornata",
            ("it", _) => "Informazione verificata aggiornata",
            ("nl", 1) => "Openingskalender bijgewerkt",
            ("nl", 2) => "Opening bijgewerkt",
            ("nl", 3) => "Bedrijfsstatus bijgewerkt",
            ("nl", 4) => "Prijs bijgewerkt",
            ("nl", 5) => "Identiteit bijgewerkt",
            ("nl", 6) => "Praktische informatie bijgewerkt",
            ("nl", _) => "Geverifieerde informatie bijgewerkt",
            ("pl", 1) => "Kalendarz otwarcia zaktualizowany",
            ("pl", 2) => "Otwarcie zaktualizowane",
            ("pl", 3) => "Status działania zaktualizowany",
            ("pl", 4) => "Cena zaktualizowana",
            ("pl", 5) => "Tożsamość zaktualizowana",
            ("pl", 6) => "Informacja praktyczna zaktualizowana",
            ("pl", _) => "Zweryfikowana informacja zaktualizowana",
            ("pt", 1) => "Calendário atualizado",
            ("pt", 2) => "Abertura atualizada",
            ("pt", 3) => "Estado de atividade atualizado",
            ("pt", 4) => "Preço atualizado",
            ("pt", 5) => "Identidade atualizada",
            ("pt", 6) => "Informação prática atualizada",
            ("pt", _) => "Informação verificada atualizada",
            (_, 1) => "Opening calendar updated",
            (_, 2) => "Opening updated",
            (_, 3) => "Operating status updated",
            (_, 4) => "Price updated",
            (_, 5) => "Identity updated",
            (_, 6) => "Practical information updated",
            _ => "Verified information updated",
        };
    }

    private static string ResolveTitle(string language, NotificationFrequency frequency)
    {
        bool weekly = frequency == NotificationFrequency.WeeklyDigest;
        return language switch
        {
            "de" => weekly ? "Dein Wochenüberblick" : "Dein Tagesüberblick",
            "es" => weekly ? "Tu resumen semanal" : "Tu resumen diario",
            "fr" => weekly ? "Ton résumé de la semaine" : "Ton résumé du jour",
            "it" => weekly ? "Il tuo riepilogo settimanale" : "Il tuo riepilogo giornaliero",
            "nl" => weekly ? "Je weekoverzicht" : "Je dagoverzicht",
            "pl" => weekly ? "Twoje podsumowanie tygodnia" : "Twoje podsumowanie dnia",
            "pt" => weekly ? "O teu resumo semanal" : "O teu resumo diário",
            _ => weekly ? "Your weekly digest" : "Your daily digest",
        };
    }

    private static string ResolveSubject(string language, NotificationFrequency frequency)
    {
        return $"{ResolveTitle(language, frequency)} · Amusement Parks";
    }

    private static string ResolvePreheader(string language, int count)
    {
        return language switch
        {
            "de" => $"{count} verifizierte Änderungen bei deinen Beobachtungen.",
            "es" => $"{count} cambios verificados en tus seguimientos.",
            "fr" => $"{count} changements vérifiés dans tes surveillances.",
            "it" => $"{count} aggiornamenti verificati nelle tue attività seguite.",
            "nl" => $"{count} geverifieerde wijzigingen in je gevolgde plekken.",
            "pl" => $"{count} zweryfikowanych zmian w obserwowanych miejscach.",
            "pt" => $"{count} alterações verificadas nas tuas subscrições.",
            _ => $"{count} verified changes in your watchlist.",
        };
    }

    private static string ResolveIntro(string language, int count)
    {
        return ResolvePreheader(language, count);
    }

    private static string ResolveProof(string language)
    {
        return language switch
        {
            "de" => "Jede Änderung stammt aus einer geprüften Quelle. Korrekturen ersetzen frühere Angaben im selben Überblick.",
            "es" => "Cada cambio procede de una fuente comprobada. Las correcciones sustituyen los datos anteriores en este mismo resumen.",
            "fr" => "Chaque changement vient d’une source contrôlée. Les corrections remplacent les anciennes informations dans ce même résumé.",
            "it" => "Ogni modifica proviene da una fonte controllata. Le correzioni sostituiscono i dati precedenti nello stesso riepilogo.",
            "nl" => "Elke wijziging komt uit een gecontroleerde bron. Correcties vervangen eerdere informatie in hetzelfde overzicht.",
            "pl" => "Każda zmiana pochodzi ze sprawdzonego źródła. Korekty zastępują wcześniejsze informacje w tym samym podsumowaniu.",
            "pt" => "Cada alteração vem de uma fonte verificada. As correções substituem os dados anteriores no mesmo resumo.",
            _ => "Every change comes from a checked source. Corrections replace earlier information in the same digest.",
        };
    }

    private static string ResolveBadge(string language)
    {
        return language switch
        {
            "de" => "Beobachtungen",
            "es" => "Seguimientos",
            "fr" => "Surveillances",
            "it" => "Attività seguite",
            "nl" => "Volglijst",
            "pl" => "Obserwowane",
            "pt" => "Subscrições",
            _ => "Watchlist",
        };
    }

    private static string ResolveManageAction(string language)
    {
        return language switch
        {
            "de" => "Benachrichtigungen verwalten",
            "es" => "Gestionar mis notificaciones",
            "fr" => "Gérer mes notifications",
            "it" => "Gestisci le notifiche",
            "nl" => "Meldingen beheren",
            "pl" => "Zarządzaj powiadomieniami",
            "pt" => "Gerir notificações",
            _ => "Manage notifications",
        };
    }

    private static string ResolveFooter(string language)
    {
        return language switch
        {
            "de" => "Du erhältst diese Nachricht nur nach deiner ausdrücklichen Zustimmung. Keine Tracking-Pixel und keine privaten Notizen.",
            "es" => "Recibes este mensaje solo con tu consentimiento explícito. Sin píxeles de seguimiento ni notas privadas.",
            "fr" => "Tu reçois ce message uniquement après ton accord explicite. Aucun pixel de suivi et aucune note privée.",
            "it" => "Ricevi questo messaggio solo dopo il tuo consenso esplicito. Nessun pixel di tracciamento e nessuna nota privata.",
            "nl" => "Je ontvangt dit bericht alleen na je uitdrukkelijke toestemming. Geen trackingpixels en geen privénotities.",
            "pl" => "Otrzymujesz tę wiadomość wyłącznie po wyraźnej zgodzie. Bez pikseli śledzących i prywatnych notatek.",
            "pt" => "Recebes esta mensagem apenas após o teu consentimento explícito. Sem píxeis de rastreio nem notas privadas.",
            _ => "You receive this message only after explicit consent. No tracking pixels and no private notes.",
        };
    }

    private static string ResolvePeriodLabel(string language)
    {
        return language switch
        {
            "de" => "Zeitraum",
            "es" => "Periodo",
            "fr" => "Période",
            "it" => "Periodo",
            "nl" => "Periode",
            "pl" => "Okres",
            "pt" => "Período",
            _ => "Period",
        };
    }

    private static string ResolveHiddenCount(string language, int count)
    {
        return language switch
        {
            "de" => $"{count} weitere Änderungen findest du im Benachrichtigungscenter.",
            "es" => $"Encontrarás {count} cambios más en el centro de notificaciones.",
            "fr" => $"Retrouve {count} autres changements dans le centre de notifications.",
            "it" => $"Trovi altri {count} aggiornamenti nel centro notifiche.",
            "nl" => $"Je vindt nog {count} wijzigingen in het meldingencentrum.",
            "pl" => $"Pozostałe zmiany ({count}) znajdziesz w centrum powiadomień.",
            "pt" => $"Encontra mais {count} alterações no centro de notificações.",
            _ => $"Find {count} more changes in the notification centre.",
        };
    }

    private static string ResolveVerified(string language)
    {
        return language switch
        {
            "de" => "verifiziert",
            "es" => "verificado",
            "fr" => "vérifié",
            "it" => "verificato",
            "nl" => "geverifieerd",
            "pl" => "zweryfikowane",
            "pt" => "verificado",
            _ => "verified",
        };
    }

    private static string ResolveCorrection(string language)
    {
        return language switch
        {
            "de" => "korrigiert",
            "es" => "corregido",
            "fr" => "corrigé",
            "it" => "corretto",
            "nl" => "gecorrigeerd",
            "pl" => "skorygowane",
            "pt" => "corrigido",
            _ => "corrected",
        };
    }

    private static string ResolveRetraction(string language)
    {
        return language switch
        {
            "de" => "zurückgezogen",
            "es" => "retirado",
            "fr" => "rétracté",
            "it" => "ritirato",
            "nl" => "ingetrokken",
            "pl" => "wycofane",
            "pt" => "retratado",
            _ => "retracted",
        };
    }

    private static string ResolveVerifiedSource(string language)
    {
        return language switch
        {
            "de" => "geprüfte Quelle",
            "es" => "fuente comprobada",
            "fr" => "source contrôlée",
            "it" => "fonte controllata",
            "nl" => "gecontroleerde bron",
            "pl" => "sprawdzone źródło",
            "pt" => "fonte verificada",
            _ => "checked source",
        };
    }

    private static string FormatPeriod(DateTime startUtc, DateTime endUtc, string language)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        return $"{startUtc.ToString("d", culture)} – {endUtc.ToString("d", culture)}";
    }

    private static string NormalizeLanguage(string? language)
    {
        string normalized = language?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "de" or "en" or "es" or "fr" or "it" or "nl" or "pl" or "pt"
            ? normalized
            : "en";
    }
}
