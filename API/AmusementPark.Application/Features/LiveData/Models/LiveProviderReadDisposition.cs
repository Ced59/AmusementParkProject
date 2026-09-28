namespace AmusementPark.Application.Features.LiveData.Models;

public enum LiveProviderReadDisposition
{
    Success = 1,
    NotModified = 2,
    RateLimited = 3,
    Unavailable = 4,
    InvalidPayload = 5,
    ResponseTooLarge = 6,
}
