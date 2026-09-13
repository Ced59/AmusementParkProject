using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialShare.Commands;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Handlers;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Core.Domain.SocialShare;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.SocialShare.Handlers;

internal sealed class StaticPublicSeoContextProvider : IPublicSeoContextProvider
{
    private readonly string publicBaseUrl;

    public StaticPublicSeoContextProvider(string publicBaseUrl)
    {
        this.publicBaseUrl = publicBaseUrl;
    }

    public Task<PublicSeoContext> GetAsync(CancellationToken cancellationToken)
    {
        PublicSeoContext context = new PublicSeoContext(this.publicBaseUrl, Array.Empty<string>());
        return Task.FromResult(context);
    }
}
