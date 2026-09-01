using AmusementPark.Application.Features.Ratings.Ports;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Ratings;

public sealed class RatingRankingFeatureSettings : IRatingRankingFeatureFlags
{
    public const string SectionName = "Ratings:Eligibility";

    public bool Enabled { get; set; }

    bool IRatingRankingFeatureFlags.EligibilityEnabled => this.Enabled;

    public static RatingRankingFeatureSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetSection(SectionName).Get<RatingRankingFeatureSettings>()
            ?? new RatingRankingFeatureSettings();
    }
}
