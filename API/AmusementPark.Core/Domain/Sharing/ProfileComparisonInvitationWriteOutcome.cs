namespace AmusementPark.Core.Domain.Sharing;

public enum ProfileComparisonInvitationWriteOutcome
{
    Success = 0,
    TokenCollision = 1,
    ConcurrencyConflict = 2,
}
