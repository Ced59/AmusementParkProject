using AmusementPark.Application.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur d'email de développement qui journalise le contenu.
/// </summary>
public sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        this.logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "Mock email sent. To: {To}
Subject: {Subject}
Body:
{Body}",
            to,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}
