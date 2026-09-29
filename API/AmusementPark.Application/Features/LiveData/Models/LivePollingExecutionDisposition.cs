namespace AmusementPark.Application.Features.LiveData.Models;

public enum LivePollingExecutionDisposition
{
    Suspended = 0,
    NotDue = 1,
    OutsideActiveWindow = 2,
    Success = 3,
    NotModified = 4,
    RateLimited = 5,
    Failed = 6,
}
