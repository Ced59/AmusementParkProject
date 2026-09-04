using AmusementPark.Infrastructure.Services.Passport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class ConfiguredGlobalRatingSuggestionFeatureGateTests
{
    [Fact]
    public void Constructor_DefaultsToEnabledAndSupportsTheKillSwitch()
    {
        ConfiguredGlobalRatingSuggestionFeatureGate defaultGate = new ConfiguredGlobalRatingSuggestionFeatureGate(
            new ConfigurationBuilder().Build());
        IConfiguration disabledConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfiguredGlobalRatingSuggestionFeatureGate.ConfigurationKey] = "false",
            })
            .Build();

        Assert.True(defaultGate.IsEnabled);
        Assert.False(new ConfiguredGlobalRatingSuggestionFeatureGate(disabledConfiguration).IsEnabled);
    }
}
