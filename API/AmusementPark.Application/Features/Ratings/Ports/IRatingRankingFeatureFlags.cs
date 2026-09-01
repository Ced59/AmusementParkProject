namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingFeatureFlags
{
    bool EligibilityEnabled { get; }
}
