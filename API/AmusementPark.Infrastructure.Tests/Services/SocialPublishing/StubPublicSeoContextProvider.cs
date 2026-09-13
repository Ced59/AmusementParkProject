using System.Net;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

internal sealed class StubPublicSeoContextProvider : IPublicSeoContextProvider
{
    public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new PublicSeoContext(
            "https://amusement-parks.fun",
            Array.Empty<string>()));
    }
}
