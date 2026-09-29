using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.Diagnostics;

public static class ApiPerformanceLogEvents
{
    public static readonly EventId RequestCompleted = new EventId(5100, nameof(RequestCompleted));

    public static readonly EventId SlowRequest = new EventId(5101, nameof(SlowRequest));

    public static readonly EventId ServerError = new EventId(5102, nameof(ServerError));
}
