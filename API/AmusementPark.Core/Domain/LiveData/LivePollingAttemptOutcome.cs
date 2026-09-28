namespace AmusementPark.Core.Domain.LiveData;

public enum LivePollingAttemptOutcome
{
    Success = 1,
    NotModified = 2,
    RateLimited = 3,
    Failure = 4,
}
