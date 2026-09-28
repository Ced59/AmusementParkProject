namespace AmusementPark.Core.Domain.LiveData;

public enum LiveQueueAvailability
{
    Unspecified = 1,
    Available = 2,
    TemporarilyFull = 3,
    Finished = 4,
    Paused = 5,
    Closed = 6,
    Unknown = 7,
}
