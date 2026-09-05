using AmusementPark.Application.Features.Ratings.Ports;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed class RecordingPublicationCacheInvalidator : IRatingRankingPublicationCacheInvalidator
{
    public int CallCount { get; private set; }

    public bool Result { get; set; } = true;

    public Task<bool> InvalidateAsync(CancellationToken cancellationToken)
    {
        this.CallCount++;
        return Task.FromResult(this.Result);
    }
}
