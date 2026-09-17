namespace AmusementPark.Core.Domain.Trips;

public enum TripPlanStatus
{
    Draft = 1,
    OpenForVotes = 2,
    Decided = 3,
    Completed = 4,
    Archived = 5,
    Cancelled = 6,
}
