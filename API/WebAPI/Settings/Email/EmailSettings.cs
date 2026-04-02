using Services.Interfaces.Settings;

namespace WebAPI.Settings.Email
{
    public class EmailSettings : IEmailSettings
    {
        public string Mode { get; set; } = "Console";

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public bool UseSsl { get; set; }

        public bool UseStartTls { get; set; } = true;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = "noreply@amusement-park.fun";

        public string FromName { get; set; } = "Amusement Park";
    }
}
