using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Services.Email;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Adaptateur d'emails fonctionnels liés au compte local.
/// </summary>
public sealed class LocalAccountEmailService : ILocalAccountEmailService
{
    private readonly IEmailSender emailSender;
    private readonly IUserAuthenticationSettings authenticationSettings;
    private readonly BrandedEmailTemplateRenderer templateRenderer;

    public LocalAccountEmailService(
        IEmailSender emailSender,
        IUserAuthenticationSettings authenticationSettings,
        BrandedEmailTemplateRenderer templateRenderer)
    {
        this.emailSender = emailSender;
        this.authenticationSettings = authenticationSettings;
        this.templateRenderer = templateRenderer;
    }

    public Task SendEmailConfirmationAsync(User user, string confirmationToken, CancellationToken cancellationToken = default)
    {
        string confirmationUrl = this.BuildUrl(user.PreferredLanguage, "confirm-account", confirmationToken);
        LocalAccountEmailCopy copy = ResolveConfirmationCopy(user.PreferredLanguage, this.authenticationSettings.EmailConfirmationTokenExpirationHours);
        string body = this.templateRenderer.Render(new BrandedEmailTemplateModel
        {
            Preheader = copy.Preheader,
            Badge = copy.Badge,
            Title = copy.Title,
            Paragraphs = copy.Paragraphs,
            Action = new BrandedEmailAction(copy.ActionLabel, confirmationUrl),
            FooterNote = copy.FooterNote,
        });

        return this.emailSender.SendAsync(user.Email ?? string.Empty, copy.Subject, body, cancellationToken);
    }

    public Task SendPasswordResetAsync(User user, string resetToken, CancellationToken cancellationToken = default)
    {
        string resetUrl = this.BuildUrl(user.PreferredLanguage, "reset-password", resetToken);
        LocalAccountEmailCopy copy = ResolvePasswordResetCopy(user.PreferredLanguage, this.authenticationSettings.PasswordResetTokenExpirationMinutes);
        string body = this.templateRenderer.Render(new BrandedEmailTemplateModel
        {
            Preheader = copy.Preheader,
            Badge = copy.Badge,
            Title = copy.Title,
            Paragraphs = copy.Paragraphs,
            Action = new BrandedEmailAction(copy.ActionLabel, resetUrl),
            FooterNote = copy.FooterNote,
        });

        return this.emailSender.SendAsync(user.Email ?? string.Empty, copy.Subject, body, cancellationToken);
    }

    private static LocalAccountEmailCopy ResolveConfirmationCopy(string? preferredLanguage, int expirationHours)
    {
        if (IsFrench(preferredLanguage))
        {
            return new LocalAccountEmailCopy(
                "Confirme ton compte Amusement Park",
                "Compte",
                "Active ton compte",
                "Encore une petite validation et ton compte sera pret.",
                new[]
                {
                    "Ton compte Amusement Park vient d'etre cree.",
                    $"Ce lien expire dans {expirationHours} heure(s).",
                },
                "Confirmer mon compte",
                "Si tu n'es pas a l'origine de cette inscription, tu peux ignorer ce message.");
        }

        return new LocalAccountEmailCopy(
            "Confirm your Amusement Park account",
            "Account",
            "Activate your account",
            "One quick check and your account will be ready.",
            new[]
            {
                "Your Amusement Park account has just been created.",
                $"This link expires in {expirationHours} hour(s).",
            },
            "Confirm my account",
            "If you did not create this account, you can ignore this email.");
    }

    private static LocalAccountEmailCopy ResolvePasswordResetCopy(string? preferredLanguage, int expirationMinutes)
    {
        if (IsFrench(preferredLanguage))
        {
            return new LocalAccountEmailCopy(
                "Reinitialise ton mot de passe Amusement Park",
                "Securite",
                "Choisis un nouveau mot de passe",
                "Voici le lien pour reprendre la main sur ton compte.",
                new[]
                {
                    "Une demande de reinitialisation de mot de passe vient d'etre faite.",
                    $"Ce lien expire dans {expirationMinutes} minute(s).",
                },
                "Changer mon mot de passe",
                "Si tu n'as pas demande ce changement, tu peux ignorer ce message.");
        }

        return new LocalAccountEmailCopy(
            "Reset your Amusement Park password",
            "Security",
            "Choose a new password",
            "Here is the link to get back into your account.",
            new[]
            {
                "A password reset was requested for your account.",
                $"This link expires in {expirationMinutes} minute(s).",
            },
            "Change my password",
            "If you did not request this change, you can ignore this email.");
    }

    private static bool IsFrench(string? preferredLanguage)
    {
        return !string.IsNullOrWhiteSpace(preferredLanguage)
            && preferredLanguage.Trim().StartsWith("fr", StringComparison.OrdinalIgnoreCase);
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

    private sealed record LocalAccountEmailCopy(
        string Subject,
        string Badge,
        string Title,
        string Preheader,
        IReadOnlyCollection<string> Paragraphs,
        string ActionLabel,
        string FooterNote);
}
