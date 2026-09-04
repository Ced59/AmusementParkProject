using AmusementPark.Application.Features.Passport.Ports;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class ConfiguredGlobalRatingSuggestionFeatureGate
    : IGlobalRatingSuggestionFeatureGate
{
    public const string ConfigurationKey =
        "Features:Passport:GlobalRatingSuggestions:Enabled";

    public ConfiguredGlobalRatingSuggestionFeatureGate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        this.IsEnabled = configuration.GetValue<bool?>(ConfigurationKey) ?? true;
    }

    public bool IsEnabled { get; }
}
