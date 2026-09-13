namespace AmusementPark.Core.Domain.Sharing;

public enum ProfileComparisonWriteOutcome
{
    Success = 1,
    TokenCollision = 2,
    ConcurrencyConflict = 3,
}
