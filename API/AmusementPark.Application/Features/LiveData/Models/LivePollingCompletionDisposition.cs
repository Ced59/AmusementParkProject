namespace AmusementPark.Application.Features.LiveData.Models;

public enum LivePollingCompletionDisposition
{
    Suspended = 0,
    OutsideActiveWindow = 1,
    Success = 2,
    NotModified = 3,
    RateLimited = 4,
    Failed = 5,
}
