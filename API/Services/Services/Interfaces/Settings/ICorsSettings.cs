namespace Services.Interfaces.Settings
{
    public interface ICorsSettings
    {
        string[] AllowedOrigins { get; set; }

        bool AllowCredentials { get; set; }
    }
}
