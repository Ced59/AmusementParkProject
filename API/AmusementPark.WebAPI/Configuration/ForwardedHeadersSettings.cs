namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Production options used to decide which reverse proxies are allowed to provide forwarded headers.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public int ForwardLimit { get; init; } = 2;

    public string[] KnownProxies { get; init; } = [];

    public string[] KnownNetworks { get; init; } = [];

    public string[] AllowedHosts { get; init; } = [];
}
