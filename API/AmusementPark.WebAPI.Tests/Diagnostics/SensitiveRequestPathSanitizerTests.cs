using AmusementPark.WebAPI.Diagnostics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Diagnostics;

public sealed class SensitiveRequestPathSanitizerTests
{
    [Theory]
    [InlineData("/public/trip-invitations/live-secret/preview", "/public/trip-invitations/[REDACTED]/preview")]
    [InlineData("/api/public/trip-invitations/live-secret/preview", "/api/public/trip-invitations/[REDACTED]/preview")]
    [InlineData("/PUBLIC/TRIP-INVITATIONS/live-secret/preview", "/PUBLIC/TRIP-INVITATIONS/[REDACTED]/preview")]
    public void Sanitize_ShouldRemoveInvitationBearerTokens(string path, string expected)
    {
        string result = SensitiveRequestPathSanitizer.Sanitize(new PathString(path));

        Assert.Equal(expected, result);
        Assert.DoesNotContain("live-secret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Sanitize_ShouldPreserveOrdinaryApiPaths()
    {
        const string path = "/public/parks/park-1";

        string result = SensitiveRequestPathSanitizer.Sanitize(new PathString(path));

        Assert.Equal(path, result);
    }

    [Theory]
    [InlineData("https://amusement-parks.fun/fr/trip-invitations/live-secret?from=email", "https://amusement-parks.fun/fr/trip-invitations/[REDACTED]")]
    [InlineData("https://amusement-parks.fun/api/public/trip-invitations/live-secret/preview/?source=page", "https://amusement-parks.fun/api/public/trip-invitations/[REDACTED]/preview/")]
    [InlineData("/de/trip-invitations/live-secret/", "/de/trip-invitations/[REDACTED]/")]
    [InlineData("https://cdn.example.com/app.js", "https://cdn.example.com/app.js")]
    [InlineData("inline", "inline")]
    [InlineData("eval", "eval")]
    [InlineData("data", "data")]
    [InlineData("data:text/plain,blocked", "data:text/plain,blocked")]
    public void SanitizeUrl_ShouldRedactInvitationTokensOnly(string value, string expected)
    {
        string? result = SensitiveRequestPathSanitizer.SanitizeUrl(value);

        Assert.Equal(expected, result);
    }
}
