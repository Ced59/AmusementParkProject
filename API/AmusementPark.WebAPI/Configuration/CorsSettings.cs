namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres de configuration CORS de l'API.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];

    public string? AllowedOriginsCsv { get; init; }

    public string[] AllowedMethods { get; init; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];

    public string[] AllowedHeaders { get; init; } = ["Authorization", "Content-Type", "Accept-Language", "X-Requested-With", "X-AmusementPark-Public-View-Mode"];

    public string[] ExposedHeaders { get; init; } = ["Retry-After", "X-Rate-Limit-Limit", "X-Rate-Limit-Remaining", "X-Rate-Limit-Reset", "X-AmusementPark-Public-View-Mode-Applied"];

    public bool AllowCredentials { get; init; } = true;
}
