namespace AmusementPark.Application.Features.FeatureFlags.Models;

public enum FeatureFlagKind
{
    Release = 1,
    OperationalKillSwitch = 2,
    Capability = 3,
    Experiment = 4,
    DataGate = 5,
}
