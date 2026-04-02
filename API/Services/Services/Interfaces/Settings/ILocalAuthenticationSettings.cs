namespace Services.Interfaces.Settings
{
    public interface ILocalAuthenticationSettings
    {
        int EmailConfirmationTokenExpirationHours { get; set; }

        int PasswordResetTokenExpirationMinutes { get; set; }

        string FrontendBaseUrl { get; set; }
    }
}
