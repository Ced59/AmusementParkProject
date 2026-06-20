using System.Globalization;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.Infrastructure.Configuration.Email;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class ContactNotificationEmailService : IContactNotificationService
{
    private readonly IEmailSender emailSender;
    private readonly EmailNotificationSettings settings;
    private readonly BrandedEmailTemplateRenderer templateRenderer;
    private readonly ILogger<ContactNotificationEmailService> logger;

    public ContactNotificationEmailService(
        IEmailSender emailSender,
        EmailNotificationSettings settings,
        BrandedEmailTemplateRenderer templateRenderer,
        ILogger<ContactNotificationEmailService> logger)
    {
        this.emailSender = emailSender;
        this.settings = settings;
        this.templateRenderer = templateRenderer;
        this.logger = logger;
    }

    public async Task NotifySubmittedAsync(ContactGrievance grievance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grievance);

        if (!this.settings.ContactNotificationsEnabled || string.IsNullOrWhiteSpace(this.settings.AdminAddress))
        {
            return;
        }

        try
        {
            string submittedAt = grievance.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
            string htmlBody = this.templateRenderer.Render(new BrandedEmailTemplateModel
            {
                Preheader = "Un nouveau message vient d'arriver depuis la page contact.",
                Badge = "Contact",
                Title = "Nouveau message contact",
                Paragraphs = new[]
                {
                    "Un visiteur a envoye un message depuis la page contact publique.",
                    $"Adresse publique a utiliser pour les echanges si besoin : {this.settings.ContactAddress}.",
                },
                Metrics = new[]
                {
                    new BrandedEmailMetric("Date", submittedAt),
                    new BrandedEmailMetric("Langue", grievance.LanguageCode ?? "unknown"),
                    new BrandedEmailMetric("IP", grievance.IpAddress ?? "unknown"),
                },
                Highlight = new BrandedEmailHighlight("Message", grievance.Message),
                FooterNote = "Notification interne envoyee a l'adresse admin configuree.",
            });

            await this.emailSender.SendAsync(
                this.settings.AdminAddress,
                "Nouveau message contact - Amusement Park",
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Contact notification email could not be sent for grievance {GrievanceId}.", grievance.Id);
        }
    }
}
