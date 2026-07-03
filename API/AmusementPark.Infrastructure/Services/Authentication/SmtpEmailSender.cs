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
    private static readonly Regex AnchorWithHrefRegex = new Regex(
        @"<\s*a\b(?=[^>]*\bhref\s*=\s*(?:""(?<href>[^""]*)""|'(?<href>[^']*)'|(?<href>[^\s>]+)))[^>]*>(?<label>.*?)<\s*/\s*a\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex LineBreakTagRegex = new Regex(
        @"<\s*br\s*/?\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex BlockClosingTagRegex = new Regex(
        @"<\s*/\s*(p|div|h1|h2|h3|li|tr)\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlTagRegex = new Regex(
        "<.*?>",
        RegexOptions.CultureInvariant);

    private static readonly Regex InlineWhitespaceRegex = new Regex(
        @"[ \t]+",
        RegexOptions.CultureInvariant);

    private static readonly Regex ExcessiveLineBreakRegex = new Regex(
        @"\n{3,}",
        RegexOptions.CultureInvariant);

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

    internal static string BuildTextBody(string htmlBody)
    {
        string withActionLinks = AnchorWithHrefRegex.Replace(htmlBody, PreserveAnchorTextAndHref);
        string withLineBreaks = LineBreakTagRegex.Replace(withActionLinks, "\n");
        string withParagraphBreaks = BlockClosingTagRegex.Replace(withLineBreaks, "\n");
        string withoutTags = HtmlTagRegex.Replace(withParagraphBreaks, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        string normalizedWhitespace = InlineWhitespaceRegex.Replace(decoded, " ");
        string normalizedLineBreaks = ExcessiveLineBreakRegex.Replace(normalizedWhitespace, "\n\n");
        return normalizedLineBreaks.Trim();
    }

    private static string PreserveAnchorTextAndHref(Match match)
    {
        string href = WebUtility.HtmlDecode(match.Groups["href"].Value).Trim();
        string label = StripHtmlFragmentToText(match.Groups["label"].Value);

        if (string.IsNullOrWhiteSpace(href))
        {
            return WebUtility.HtmlEncode(label);
        }

        if (string.IsNullOrWhiteSpace(label) || string.Equals(label, href, StringComparison.Ordinal))
        {
            return WebUtility.HtmlEncode(href);
        }

        return $"{WebUtility.HtmlEncode(label)}\n{WebUtility.HtmlEncode(href)}";
    }

    private static string StripHtmlFragmentToText(string htmlFragment)
    {
        string withLineBreaks = LineBreakTagRegex.Replace(htmlFragment, "\n");
        string withoutTags = HtmlTagRegex.Replace(withLineBreaks, " ");
        string decoded = WebUtility.HtmlDecode(withoutTags);
        string normalizedWhitespace = InlineWhitespaceRegex.Replace(decoded, " ");
        return normalizedWhitespace.Trim();
    }
}
