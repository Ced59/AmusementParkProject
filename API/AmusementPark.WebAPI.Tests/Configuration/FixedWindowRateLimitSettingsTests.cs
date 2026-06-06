using AmusementPark.WebAPI.Configuration;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Configuration;

public sealed class FixedWindowRateLimitSettingsTests
{
    [Fact]
    public void Create_WhenValuesProvided_ShouldBuildSettings()
    {
        FixedWindowRateLimitSettings settings = FixedWindowRateLimitSettings.Create(5, 60);

        Assert.Equal(5, settings.PermitLimit);
        Assert.Equal(60, settings.WindowSeconds);
    }
}
