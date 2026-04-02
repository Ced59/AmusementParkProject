using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Services.Interfaces.Authentication;
using Services.Interfaces.Settings;

namespace Services.Implementations.Authentication
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IEmailSettings emailSettings;
        private readonly ILogger<SmtpEmailSender> logger;

        public SmtpEmailSender(
            IEmailSettings emailSettings,
            ILogger<SmtpEmailSender> logger)
        {
            this.emailSettings = emailSettings;
            this.logger = logger;
        }

        public async Task SendAsync(string to, string subject, string plainTextBody, CancellationToken cancellationToken = default)
        {
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings.FromName, emailSettings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            BodyBuilder bodyBuilder = new BodyBuilder
            {
                TextBody = plainTextBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using SmtpClient client = new SmtpClient();

            SecureSocketOptions socketOptions = ResolveSocketOptions();

            await client.ConnectAsync(emailSettings.Host, emailSettings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(emailSettings.Username))
            {
                await client.AuthenticateAsync(emailSettings.Username, emailSettings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Email sent using SMTP provider to {To} with subject {Subject}.", to, subject);
        }

        private SecureSocketOptions ResolveSocketOptions()
        {
            if (emailSettings.UseSsl)
            {
                return SecureSocketOptions.SslOnConnect;
            }

            if (emailSettings.UseStartTls)
            {
                return SecureSocketOptions.StartTls;
            }

            return SecureSocketOptions.Auto;
        }
    }
}
