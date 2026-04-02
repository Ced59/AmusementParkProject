using Services.Interfaces.Settings;

namespace WebAPI.Settings.Security
{
    public class LocalAuthenticationSettings : ILocalAuthenticationSettings
    {
        public int EmailConfirmationTokenExpirationHours { get; set; } = 24;

        public int PasswordResetTokenExpirationMinutes { get; set; } = 60;

        public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
    }
}
