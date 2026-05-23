using System.Collections.Generic;

namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Configuration of the HTTP Content-Security-Policy header.
/// </summary>
public sealed class ContentSecurityPolicySettings
{
    public const string SectionName = "Security:ContentSecurityPolicy";

    public bool Enabled { get; init; } = true;

    public bool ReportOnly { get; init; } = true;

    public string ReportUri { get; init; } = "/security/csp-report";

    public string Policy { get; init; } = string.Empty;

    public Dictionary<string, string[]> Directives { get; init; } = new Dictionary<string, string[]>();
}
