using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Security;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Security;

public sealed class ContentSecurityPolicyHeaderBuilderTests
{
    [Fact]
    public void Build_WhenPolicyOverrideIsProvided_ShouldNormalizePolicyAndIgnoreDirectives()
    {
        ContentSecurityPolicySettings settings = new ContentSecurityPolicySettings
        {
            Policy = " default-src 'self' ; ; script-src 'self'  ",
            Directives = new Dictionary<string, string[]>
            {
                ["img-src"] = new[] { "*" },
            },
        };

        string result = ContentSecurityPolicyHeaderBuilder.Build(settings);

        Assert.Equal("default-src 'self'; script-src 'self'", result);
    }

    [Fact]
    public void Build_WhenDirectivesAreProvided_ShouldRenderPreferredOrderThenRemainingDirectives()
    {
        ContentSecurityPolicySettings settings = new ContentSecurityPolicySettings
        {
            ReportUri = string.Empty,
            Directives = new Dictionary<string, string[]>
            {
                ["img-src"] = new[] { " data: ", "data:", "https://cdn.example.com" },
                ["default-src"] = new[] { "'self'" },
                ["custom-src"] = new[] { "https://custom.example.com" },
            },
        };

        string result = ContentSecurityPolicyHeaderBuilder.Build(settings);

        Assert.Equal("default-src 'self'; img-src data: https://cdn.example.com; custom-src https://custom.example.com", result);
    }

    [Fact]
    public void Build_WhenDirectiveHasNoSources_ShouldRenderDirectiveNameOnly()
    {
        ContentSecurityPolicySettings settings = new ContentSecurityPolicySettings
        {
            ReportUri = string.Empty,
            Directives = new Dictionary<string, string[]>
            {
                ["upgrade-insecure-requests"] = Array.Empty<string>(),
            },
        };

        string result = ContentSecurityPolicyHeaderBuilder.Build(settings);

        Assert.Equal("upgrade-insecure-requests", result);
    }

    [Fact]
    public void Build_WhenReportUriConfiguredAndNoReportDirectiveExists_ShouldAppendReportUri()
    {
        ContentSecurityPolicySettings settings = new ContentSecurityPolicySettings
        {
            ReportUri = " /security/csp-report ",
            Directives = new Dictionary<string, string[]>
            {
                ["default-src"] = new[] { "'self'" },
            },
        };

        string result = ContentSecurityPolicyHeaderBuilder.Build(settings);

        Assert.Equal("default-src 'self'; report-uri /security/csp-report", result);
    }

    [Fact]
    public void Build_WhenReportUriDirectiveAlreadyExists_ShouldNotAppendDuplicateReportUri()
    {
        ContentSecurityPolicySettings settings = new ContentSecurityPolicySettings
        {
            ReportUri = "/security/csp-report",
            Directives = new Dictionary<string, string[]>
            {
                ["report-uri"] = new[] { "/already" },
            },
        };

        string result = ContentSecurityPolicyHeaderBuilder.Build(settings);

        Assert.Equal("report-uri /already", result);
    }

    [Fact]
    public void Build_WhenSettingsIsNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ContentSecurityPolicyHeaderBuilder.Build(null!));
    }
}
