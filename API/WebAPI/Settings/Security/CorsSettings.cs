using Services.Interfaces.Settings;

namespace WebAPI.Settings.Security
{
    public class CorsSettings : ICorsSettings
    {
        public string[] AllowedOrigins { get; set; } = new[] { "http://localhost:4200" };

        public bool AllowCredentials { get; set; } = true;
    }
}
