using AmusementPark.Application.Features.Seo.Ports;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class PublicSeoResponseCacheInvalidator : IPublicSeoResponseCacheInvalidator
{
    private readonly IOutputCacheStore outputCacheStore;

    public PublicSeoResponseCacheInvalidator(IOutputCacheStore outputCacheStore)
    {
        this.outputCacheStore = outputCacheStore;
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        await this.outputCacheStore.EvictByTagAsync(
            ApiOutputCachePolicyNames.PublicSeoTag,
            cancellationToken);
    }
}
