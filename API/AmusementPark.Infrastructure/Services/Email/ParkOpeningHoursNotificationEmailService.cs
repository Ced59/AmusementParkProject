using System.Globalization;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Configuration.Email;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class ParkOpeningHoursNotificationEmailService : IParkOpeningHoursNotificationService
{
    private readonly IEmailSender emailSender;
    private readonly EmailNotificationSettings settings;
    private readonly BrandedEmailTemplateRenderer templateRenderer;
    private readonly ILogger<ParkOpeningHoursNotificationEmailService> logger;

    public ParkOpeningHoursNotificationEmailService(
        IEmailSender emailSender,
        EmailNotificationSettings settings,
        BrandedEmailTemplateRenderer templateRenderer,
        ILogger<ParkOpeningHoursNotificationEmailService> logger)
    {
        this.emailSender = emailSender;
        this.settings = settings;
        this.templateRenderer = templateRenderer;
        this.logger = logger;
    }

    public async Task NotifyCoverageThresholdReachedAsync(ParkOpeningHoursCoverageNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (!this.settings.OpeningHoursCoverageNotificationsEnabled || string.IsNullOrWhiteSpace(this.settings.AdminAddress))
        {
            return;
        }

        try
        {
            string title = notification.ThresholdDays == 0
                ? "Horaires expires"
                : "Horaires a completer";
            string paragraph = notification.ThresholdDays == 0
                ? "Les horaires renseignes pour ce parc ne couvrent plus la date locale courante."
                : "Les horaires renseignes pour ce parc arrivent au seuil de couverture configure.";

            string htmlBody = this.templateRenderer.Render(new BrandedEmailTemplateModel
            {
                Preheader = $"Alerte horaires pour {notification.ParkName}.",
                Badge = "Horaires",
                Title = title,
                Paragraphs = new[]
                {
                    paragraph,
                    "Cette notification concerne uniquement un parc avec des horaires deja renseignes.",
                },
                Metrics = new[]
                {
                    new BrandedEmailMetric("Parc", notification.ParkName),
                    new BrandedEmailMetric("Park ID", notification.ParkId),
                    new BrandedEmailMetric("Jours complets", notification.CompleteForDays.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Seuil", notification.ThresholdDays.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Couvert jusqu'au", FormatDate(notification.CompleteUntilDate)),
                    new BrandedEmailMetric("Date locale", FormatDate(notification.LocalDate)),
                    new BrandedEmailMetric("Fuseau", notification.TimeZoneId),
                },
                FooterNote = "Notification interne de maintenance des horaires de parcs.",
            });

            await this.emailSender.SendAsync(
                this.settings.AdminAddress,
                ResolveSubject(notification),
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Opening hours coverage notification email could not be sent for park {ParkId} and threshold {ThresholdDays}.", notification.ParkId, notification.ThresholdDays);
        }
    }

    private static string ResolveSubject(ParkOpeningHoursCoverageNotification notification)
    {
        return notification.ThresholdDays == 0
            ? "Horaires expires - Amusement Park"
            : "Horaires a completer - Amusement Park";
    }

    private static string FormatDate(DateOnly? value)
    {
        return value.HasValue
            ? FormatDate(value.Value)
            : "non disponible";
    }

    private static string FormatDate(DateOnly value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
