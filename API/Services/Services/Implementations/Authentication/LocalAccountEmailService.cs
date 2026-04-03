using System;
using System.Threading;
using System.Threading.Tasks;
using Entities.Model.Users;
using Services.Interfaces.Authentication;
using Services.Interfaces.Settings;

namespace Services.Implementations.Authentication
{
    public class LocalAccountEmailService : ILocalAccountEmailService
    {
        private readonly IEmailSender emailSender;
        private readonly ILocalAuthenticationSettings localAuthenticationSettings;

        public LocalAccountEmailService(
            IEmailSender emailSender,
            ILocalAuthenticationSettings localAuthenticationSettings)
        {
            this.emailSender = emailSender;
            this.localAuthenticationSettings = localAuthenticationSettings;
        }

        public Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default)
        {
            string confirmationUrl = BuildUrl(user.PreferredLanguage, "confirm-account", confirmationToken);
            string subject = "Confirm your Amusement Park account";
            string body = $"""
A new Amusement Park account has been created for {user.Email}.

To activate the account, open the following URL:
{confirmationUrl}

This link expires in {localAuthenticationSettings.EmailConfirmationTokenExpirationHours} hour(s).
""";

            return emailSender.SendAsync(user.Email ?? string.Empty, subject, body, cancellationToken);
        }

        public Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default)
        {
            string resetUrl = BuildUrl(user.PreferredLanguage, "reset-password", resetToken);
            string subject = "Reset your Amusement Park password";
            string body = $"""
A password reset was requested for {user.Email}.

To choose a new password, open the following URL:
{resetUrl}

This link expires in {localAuthenticationSettings.PasswordResetTokenExpirationMinutes} minute(s).
If you did not request this change, you can ignore this email.
""";

            return emailSender.SendAsync(user.Email ?? string.Empty, subject, body, cancellationToken);
        }

        private string BuildUrl(string? preferredLanguage, string routeSegment, string token)
        {
            string language = string.IsNullOrWhiteSpace(preferredLanguage)
                ? "en"
                : preferredLanguage.Trim().ToLowerInvariant();

            string baseUrl = localAuthenticationSettings.FrontendBaseUrl.TrimEnd('/');
            string encodedToken = Uri.EscapeDataString(token);
            return $"{baseUrl}/{language}/{routeSegment}?token={encodedToken}";
        }
    }
}
