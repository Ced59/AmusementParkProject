using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Email;

public sealed class EmailNotificationSettings
{
    public const string SectionName = "Email:Notifications";

    public string AdminAddress { get; set; } = "admin@amusement-parks.fun";

    public string ContactAddress { get; set; } = "contact@amusement-parks.fun";

    public bool ContactNotificationsEnabled { get; set; } = true;

    public bool WeatherRunNotificationsEnabled { get; set; } = true;

    public bool OpeningHoursCoverageNotificationsEnabled { get; set; } = true;

    public string OpeningHoursCoverageNotificationTimeZoneId { get; set; } = "Europe/Paris";

    public int OpeningHoursCoverageNotificationHour { get; set; } = 8;

    public int OpeningHoursCoverageNotificationMinute { get; set; } = 30;

    public static EmailNotificationSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        EmailNotificationSettings settings = configuration.GetSection(SectionName).Get<EmailNotificationSettings>() ?? new EmailNotificationSettings();
        settings.AdminAddress = NormalizeAddress(settings.AdminAddress, "admin@amusement-parks.fun");
        settings.ContactAddress = NormalizeAddress(settings.ContactAddress, "contact@amusement-parks.fun");
        settings.OpeningHoursCoverageNotificationHour = Math.Clamp(settings.OpeningHoursCoverageNotificationHour, 0, 23);
        settings.OpeningHoursCoverageNotificationMinute = Math.Clamp(settings.OpeningHoursCoverageNotificationMinute, 0, 59);
        if (string.IsNullOrWhiteSpace(settings.OpeningHoursCoverageNotificationTimeZoneId))
        {
            settings.OpeningHoursCoverageNotificationTimeZoneId = "Europe/Paris";
        }

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
