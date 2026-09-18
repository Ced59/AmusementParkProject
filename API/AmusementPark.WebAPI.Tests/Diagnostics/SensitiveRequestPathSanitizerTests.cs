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
}
