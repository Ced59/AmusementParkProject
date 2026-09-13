using System.Net.Http.Headers;

namespace AmusementPark.Infrastructure.Services.DataSources.Acquisition;

/// <summary>
/// Options réseau génériques d'acquisition.
/// </summary>
internal sealed class DataAcquisitionRequestOptions
{
    public int DelayBetweenRequestsMs { get; init; } = 1000;

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;
}
