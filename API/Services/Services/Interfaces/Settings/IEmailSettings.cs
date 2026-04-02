namespace Services.Interfaces.Settings
{
    public interface IEmailSettings
    {
        string Mode { get; set; }

        string Host { get; set; }

        int Port { get; set; }

        bool UseSsl { get; set; }

        bool UseStartTls { get; set; }

        string Username { get; set; }

        string Password { get; set; }

        string FromAddress { get; set; }

        string FromName { get; set; }
    }
}
