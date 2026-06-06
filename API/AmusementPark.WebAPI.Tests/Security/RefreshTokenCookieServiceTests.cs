using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Security;

public sealed class RefreshTokenCookieServiceTests
{
    [Fact]
    public void AppendRefreshTokenCookie_WhenResponseIsNull_ShouldThrow()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());

        Assert.Throws<ArgumentNullException>(() => service.AppendRefreshTokenCookie(null!, "token", DateTime.UtcNow.AddDays(1)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AppendRefreshTokenCookie_WhenTokenIsBlank_ShouldNotAppendCookie(string? refreshToken)
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();

        service.AppendRefreshTokenCookie(httpContext.Response, refreshToken!, DateTime.UtcNow.AddDays(1));

        Assert.Equal(0, httpContext.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void AppendRefreshTokenCookie_WhenTokenIsValid_ShouldAppendConfiguredHttpOnlyCookie()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();
        DateTime expiresAtUtc = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        service.AppendRefreshTokenCookie(httpContext.Response, "refresh-token-value", expiresAtUtc);

        string setCookie = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("ap_refresh=refresh-token-value", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/auth", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("domain=example.com", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendRefreshTokenCookie_WhenPathAndDomainAreBlank_ShouldUseRootPathAndNoDomain()
    {
        RefreshTokenCookieSettings settings = CreateSettings();
        settings = new RefreshTokenCookieSettings
        {
            Name = settings.Name,
            Path = "   ",
            Domain = "   ",
            SameSite = settings.SameSite,
            Secure = settings.Secure,
            HttpOnly = settings.HttpOnly,
        };
        RefreshTokenCookieService service = new RefreshTokenCookieService(settings);
        DefaultHttpContext httpContext = new DefaultHttpContext();

        service.AppendRefreshTokenCookie(httpContext.Response, "refresh-token-value", DateTime.UtcNow.AddDays(1));

        string setCookie = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("path=/", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRefreshToken_WhenRequestIsNull_ShouldThrow()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());

        Assert.Throws<ArgumentNullException>(() => service.GetRefreshToken(null!));
    }

    [Fact]
    public void GetRefreshToken_WhenCookieExists_ShouldReturnCookieValue()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "ap_refresh=stored-token; other=value";

        string? result = service.GetRefreshToken(httpContext.Request);

        Assert.Equal("stored-token", result);
    }

    [Fact]
    public void GetRefreshToken_WhenCookieIsMissing_ShouldReturnNull()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();

        string? result = service.GetRefreshToken(httpContext.Request);

        Assert.Null(result);
    }

    [Fact]
    public void GetRefreshToken_WhenCookieIsBlank_ShouldReturnNull()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "ap_refresh=   ";

        string? result = service.GetRefreshToken(httpContext.Request);

        Assert.Null(result);
    }

    [Fact]
    public void DeleteRefreshTokenCookie_WhenResponseIsNull_ShouldThrow()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());

        Assert.Throws<ArgumentNullException>(() => service.DeleteRefreshTokenCookie(null!));
    }

    [Fact]
    public void DeleteRefreshTokenCookie_WhenCalled_ShouldEmitExpiredCookieWithConfiguredName()
    {
        RefreshTokenCookieService service = new RefreshTokenCookieService(CreateSettings());
        DefaultHttpContext httpContext = new DefaultHttpContext();

        service.DeleteRefreshTokenCookie(httpContext.Response);

        string setCookie = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("ap_refresh=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/auth", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    private static RefreshTokenCookieSettings CreateSettings()
    {
        return new RefreshTokenCookieSettings
        {
            Name = "ap_refresh",
            Domain = "example.com",
            Path = "/auth",
            SameSite = "Strict",
            Secure = true,
            HttpOnly = true,
        };
    }
}
