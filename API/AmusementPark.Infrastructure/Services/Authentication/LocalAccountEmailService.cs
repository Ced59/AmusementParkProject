using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur d'emails fonctionnels liés au compte local.
/// </summary>
public sealed class LocalAccountEmailService : ILocalAccountEmailService
{
    private readonly IEmailSender emailSender;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public LocalAccountEmailService(IEmailSender emailSender, IUserAuthenticationSettings authenticationSettings)
    {
        this.emailSender = emailSender;
        this.authenticationSettings = authenticationSettings;
    }

    public Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default)
    {
        string confirmationUrl = this.BuildUrl(user.PreferredLanguage, "confirm-account", confirmationToken);
        string subject = "Confirm your Amusement Park account";
        string body = $"""
A new Amusement Park account has been created for {user.Email}.

To activate the account, open the following URL:
{confirmationUrl}

This link expires in {this.authenticationSettings.EmailConfirmationTokenExpirationHours} hour(s).
""";

        return this.emailSender.SendAsync(user.Email ?? string.Empty, subject, body, cancellationToken);
    }

    public Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default)
    {
        string resetUrl = this.BuildUrl(user.PreferredLanguage, "reset-password", resetToken);
        string subject = "Reset your Amusement Park password";
        string body = $"""
A password reset was requested for {user.Email}.

To choose a new password, open the following URL:
{resetUrl}

This link expires in {this.authenticationSettings.PasswordResetTokenExpirationMinutes} minute(s).
If you did not request this change, you can ignore this email.
""";

        return this.emailSender.SendAsync(user.Email ?? string.Empty, subject, body, cancellationToken);
    }

    private string BuildUrl(string? preferredLanguage, string routeSegment, string token)
    {
        string language = string.IsNullOrWhiteSpace(preferredLanguage)
            ? "en"
            : preferredLanguage.Trim().ToLowerInvariant();

        string baseUrl = this.authenticationSettings.FrontendBaseUrl.TrimEnd('/');
        string encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/{language}/{routeSegment}?token={encodedToken}";
    }
}
