using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialPublishing.Services;

internal sealed class StubPublicSeoContextProvider : IPublicSeoContextProvider
{
    public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new PublicSeoContext(
            "https://amusement-parks.fun",
            new[] { "en", "fr" }));
    }
}
