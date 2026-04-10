namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres de configuration CORS de l'API.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = ["http://localhost:4200"];

    public bool AllowCredentials { get; init; } = true;
}
