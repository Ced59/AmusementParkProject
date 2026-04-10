using AmusementPark.Infrastructure.Configuration.Authentication;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using AmusementPark.Application.Ports;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur SMTP réel pour l'envoi des emails applicatifs.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings emailSettings;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(EmailSettings emailSettings, ILogger<SmtpEmailSender> logger)
    {
        this.emailSettings = emailSettings;
        this.logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(this.emailSettings.FromName, this.emailSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        BodyBuilder bodyBuilder = new BodyBuilder
        {
            TextBody = htmlBody,
        };

        message.Body = bodyBuilder.ToMessageBody();

        using SmtpClient client = new SmtpClient();
        await client.ConnectAsync(this.emailSettings.Host, this.emailSettings.Port, this.ResolveSocketOptions(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(this.emailSettings.Username))
        {
            await client.AuthenticateAsync(this.emailSettings.Username, this.emailSettings.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        this.logger.LogInformation("Email sent using SMTP provider to {To} with subject {Subject}.", to, subject);
    }

    private SecureSocketOptions ResolveSocketOptions()
    {
        if (this.emailSettings.UseSsl)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (this.emailSettings.UseStartTls)
        {
            return SecureSocketOptions.StartTls;
        }

        return SecureSocketOptions.Auto;
    }
}
