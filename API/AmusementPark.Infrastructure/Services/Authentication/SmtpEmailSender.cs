using System.Net;
using System.Text.RegularExpressions;
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
            HtmlBody = htmlBody,
            TextBody = BuildTextBody(htmlBody),
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

    private static string BuildTextBody(string htmlBody)
    {
        string withLineBreaks = Regex.Replace(htmlBody, @"<\s*br\s*/?\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string withParagraphBreaks = Regex.Replace(withLineBreaks, @"<\s*/\s*(p|div|h1|h2|h3|li|tr)\s*>", "\n", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string withoutTags = Regex.Replace(withParagraphBreaks, "<.*?>", " ", RegexOptions.CultureInvariant);
        string decoded = WebUtility.HtmlDecode(withoutTags);
        string normalizedWhitespace = Regex.Replace(decoded, @"[ \t]+", " ", RegexOptions.CultureInvariant);
        string normalizedLineBreaks = Regex.Replace(normalizedWhitespace, @"\n{3,}", "\n\n", RegexOptions.CultureInvariant);
        return normalizedLineBreaks.Trim();
    }
}
