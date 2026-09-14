namespace AmusementPark.Core.Domain.ParkFit;

public enum ParkFitGroupProfileWriteOutcome
{
    Success = 1,
    AliasConflict = 2,
    ConcurrencyConflict = 3,
    LimitReached = 4,
}
