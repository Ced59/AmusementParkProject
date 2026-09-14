namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// État de publication du score comparatif.
/// </summary>
public enum ParkFitScoreState
{
    Available,
    Capped,
    Suspended,
    Excluded,
}
