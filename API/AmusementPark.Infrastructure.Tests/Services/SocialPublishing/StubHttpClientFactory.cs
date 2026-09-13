using System.Net;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Infrastructure.Configuration.SocialPublishing;
using AmusementPark.Infrastructure.Services.SocialPublishing;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.SocialPublishing;

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient client;

    public StubHttpClientFactory(HttpMessageHandler handler)
    {
        this.client = new HttpClient(handler);
    }

    public string? LastClientName { get; private set; }

    public List<string> ClientNames { get; } = new List<string>();

    public HttpClient CreateClient(string name)
    {
        this.LastClientName = name;
        this.ClientNames.Add(name);
        return this.client;
    }
}
