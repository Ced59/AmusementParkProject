using AmusementPark.WebAPI.Configuration;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Configuration;

public sealed class AuthenticationRateLimitingSettingsTests
{
    [Fact]
    public void Constructor_WhenCreated_ShouldExposeExpectedDefaultPolicies()
    {
        AuthenticationRateLimitingSettings settings = new AuthenticationRateLimitingSettings();

        Assert.Equal(5, settings.Login.PermitLimit);
        Assert.Equal(60, settings.Login.WindowSeconds);
        Assert.Equal(10, settings.ExternalLogin.PermitLimit);
        Assert.Equal(30, settings.RefreshToken.PermitLimit);
        Assert.Equal(900, settings.Registration.WindowSeconds);
        Assert.Equal(900, settings.EmailChallenge.WindowSeconds);
        Assert.Equal(900, settings.PasswordReset.WindowSeconds);
    }
}
