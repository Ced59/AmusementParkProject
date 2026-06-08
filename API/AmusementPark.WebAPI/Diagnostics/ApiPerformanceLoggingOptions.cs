namespace AmusementPark.WebAPI.Diagnostics;

/// <summary>
/// Options de journalisation ciblée des requêtes API lentes.
/// </summary>
public sealed class ApiPerformanceLoggingOptions
{
    public const string SectionName = "Diagnostics:PerformanceLogging";

    public bool Enabled { get; set; } = true;

    public bool LogAllRequests { get; set; }

    public int SlowRequestThresholdMilliseconds { get; set; } = 750;

    public int AlwaysLogStatusCodeAtLeast { get; set; } = 500;

    public string[] ExcludedPathPrefixes { get; set; } = new[]
    {
        "/health",
        "/favicon.ico"
    };
}
