using System.Globalization;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Email;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class ParkWeatherNotificationEmailService : IParkWeatherNotificationService
{
    private readonly IEmailSender emailSender;
    private readonly EmailNotificationSettings settings;
    private readonly BrandedEmailTemplateRenderer templateRenderer;
    private readonly ILogger<ParkWeatherNotificationEmailService> logger;

    public ParkWeatherNotificationEmailService(
        IEmailSender emailSender,
        EmailNotificationSettings settings,
        BrandedEmailTemplateRenderer templateRenderer,
        ILogger<ParkWeatherNotificationEmailService> logger)
    {
        this.emailSender = emailSender;
        this.settings = settings;
        this.templateRenderer = templateRenderer;
        this.logger = logger;
    }

    public async Task NotifyRunStartedAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!this.ShouldSend(run))
        {
            return;
        }

        try
        {
            string htmlBody = this.templateRenderer.Render(new BrandedEmailTemplateModel
            {
                Preheader = "Le batch meteo quotidien vient de demarrer.",
                Badge = "Meteo",
                Title = "Batch meteo lance",
                Paragraphs = new[]
                {
                    "Le rafraichissement quotidien de la meteo des parcs vient de demarrer.",
                    "Un second email sera envoye lorsque le traitement sera termine.",
                },
                Metrics = new[]
                {
                    new BrandedEmailMetric("Run", run.Id ?? "unknown"),
                    new BrandedEmailMetric("Demarrage", FormatDateTime(run.StartedAtUtc ?? DateTime.UtcNow)),
                    new BrandedEmailMetric("Portee", run.Scope.ToString()),
                },
                FooterNote = "Notification interne du batch meteo automatique.",
            });

            await this.emailSender.SendAsync(
                this.settings.AdminAddress,
                "Batch meteo lance - Amusement Park",
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Weather start notification email could not be sent for run {RunId}.", run.Id);
        }
    }

    public async Task NotifyRunCompletedAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!this.ShouldSend(run))
        {
            return;
        }

        try
        {
            string title = ResolveCompletionTitle(run.Status);
            string htmlBody = this.templateRenderer.Render(new BrandedEmailTemplateModel
            {
                Preheader = $"Le batch meteo quotidien est termine avec le statut {run.Status}.",
                Badge = "Meteo",
                Title = title,
                Paragraphs = new[]
                {
                    "Le rafraichissement quotidien de la meteo des parcs est termine.",
                    ResolveCompletionMessage(run),
                },
                Metrics = new[]
                {
                    new BrandedEmailMetric("Statut", run.Status.ToString()),
                    new BrandedEmailMetric("Parcs traites", run.TotalParkCount.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Succes", run.SucceededParkCount.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Echecs", run.FailedParkCount.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Alertes", run.WarningParkCount.ToString(CultureInfo.InvariantCulture)),
                    new BrandedEmailMetric("Fin", FormatDateTime(run.CompletedAtUtc ?? DateTime.UtcNow)),
                },
                Highlight = string.IsNullOrWhiteSpace(run.Message)
                    ? null
                    : new BrandedEmailHighlight("Message technique", run.Message),
                FooterNote = "Notification interne du batch meteo automatique.",
            });

            await this.emailSender.SendAsync(
                this.settings.AdminAddress,
                "Resultat batch meteo - Amusement Park",
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Weather completion notification email could not be sent for run {RunId}.", run.Id);
        }
    }

    private bool ShouldSend(ParkWeatherRun run)
    {
        return this.settings.WeatherRunNotificationsEnabled
            && run.Trigger == ParkWeatherRunTrigger.Automatic
            && !string.IsNullOrWhiteSpace(this.settings.AdminAddress);
    }

    private static string ResolveCompletionTitle(ParkWeatherRunStatus status)
    {
        return status switch
        {
            ParkWeatherRunStatus.Completed => "Batch meteo termine",
            ParkWeatherRunStatus.CompletedWithFailures => "Batch meteo termine avec echecs",
            ParkWeatherRunStatus.Skipped => "Batch meteo ignore",
            ParkWeatherRunStatus.Failed => "Batch meteo en echec",
            _ => "Batch meteo termine",
        };
    }

    private static string ResolveCompletionMessage(ParkWeatherRun run)
    {
        if (run.Status == ParkWeatherRunStatus.Completed)
        {
            return "Tout est passe correctement pour les parcs inclus dans le batch.";
        }

        if (run.Status == ParkWeatherRunStatus.Skipped)
        {
            return "Le batch automatique a ete ignore parce qu'un traitement manuel couvrait deja ce cycle.";
        }

        if (run.FailedParkCount > 0)
        {
            return "Au moins un parc n'a pas pu etre rafraichi. Le detail reste disponible dans l'administration.";
        }

        return "Le statut final demande une verification dans l'administration.";
    }

    private static string FormatDateTime(DateTime value)
    {
        DateTime utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        return utcValue.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
    }
}
