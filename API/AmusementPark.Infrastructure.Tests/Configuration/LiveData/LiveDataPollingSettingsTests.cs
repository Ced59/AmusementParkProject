using AmusementPark.Infrastructure.Configuration.LiveData;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Configuration.LiveData;

public sealed class LiveDataPollingSettingsTests
{
    [Fact]
    public void Bind_WithoutConfiguration_ShouldKeepPilotDisabled()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        LiveDataPollingSettings settings = LiveDataPollingSettings.Bind(configuration);

        Assert.False(settings.Enabled);
        Assert.True(settings.OperationalMutationsEnabled);
        Assert.True(settings.IsEnabled);
        Assert.Empty(settings.Targets);
        Assert.Empty(settings.BuildEnabledTargets());
    }

    [Fact]
    public void Bind_WhenDeploymentDisablesMutations_ShouldExposeFailClosedAvailability()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LiveDataPolling:OperationalMutationsEnabled"] = "false",
            })
            .Build();

        LiveDataPollingSettings settings = LiveDataPollingSettings.Bind(configuration);

        Assert.False(settings.OperationalMutationsEnabled);
        Assert.False(settings.IsEnabled);
    }

    [Fact]
    public void Validate_WhenEnabledWithoutTarget_ShouldRejectConfiguration()
    {
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = true,
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            settings.Validate);

        Assert.Contains("Exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenPilotContainsTwoTargets_ShouldRejectConfiguration()
    {
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Targets = new List<LiveDataPollingTargetSettings>
            {
                CreateTarget("one"),
                CreateTarget("two"),
            },
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            settings.Validate);

        Assert.Contains("at most one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenPollingIntervalIsUnderFiveMinutes_ShouldRejectConfiguration()
    {
        LiveDataPollingTargetSettings target = CreateTarget("one");
        target.PollingIntervalSeconds = 299;
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = true,
            Targets = new List<LiveDataPollingTargetSettings> { target },
        };

        Assert.ThrowsAny<ArgumentException>(settings.Validate);
    }

    [Fact]
    public void Validate_WhenWindowStartsAtNight_ShouldRejectConfiguration()
    {
        LiveDataPollingTargetSettings target = CreateTarget("one");
        target.ActiveFromHourLocal = 2;
        LiveDataPollingSettings settings = new LiveDataPollingSettings
        {
            Enabled = true,
            Targets = new List<LiveDataPollingTargetSettings> { target },
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            settings.Validate);

        Assert.Contains("night polling", exception.Message, StringComparison.Ordinal);
    }

    private static LiveDataPollingTargetSettings CreateTarget(string externalEntityId)
    {
        return new LiveDataPollingTargetSettings
        {
            Enabled = true,
            SourceId = "themeparks-wiki",
            ExternalEntityId = externalEntityId,
            TimeZoneId = "UTC",
        };
    }
}
