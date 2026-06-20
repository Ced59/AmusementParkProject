using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Email;

public sealed class EmailNotificationSettings
{
    public const string SectionName = "Email:Notifications";

    public string AdminAddress { get; set; } = "admin@amusement-parks.fun";

    public string ContactAddress { get; set; } = "contact@amusement-parks.fun";

    public bool ContactNotificationsEnabled { get; set; } = true;

    public bool WeatherRunNotificationsEnabled { get; set; } = true;

    public static EmailNotificationSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        EmailNotificationSettings settings = configuration.GetSection(SectionName).Get<EmailNotificationSettings>() ?? new EmailNotificationSettings();
        settings.AdminAddress = NormalizeAddress(settings.AdminAddress, "admin@amusement-parks.fun");
        settings.ContactAddress = NormalizeAddress(settings.ContactAddress, "contact@amusement-parks.fun");
        return settings;
    }

    private static string NormalizeAddress(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim();
    }
}
