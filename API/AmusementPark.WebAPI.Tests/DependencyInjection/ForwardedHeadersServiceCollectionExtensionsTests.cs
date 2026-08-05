using System.Collections.Generic;
using System.Net;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class ForwardedHeadersServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddApiForwardedHeaders_WhenTrustedProxyChainUsesIpv4MappedToIpv6_ShouldResolvePublicClientAddress()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:ForwardLimit"] = "2",
                ["ForwardedHeaders:KnownNetworks:0"] = "172.30.31.0/24",
                ["ForwardedHeaders:KnownNetworks:1"] = "172.19.0.0/16",
                ["ForwardedHeaders:AllowedHosts:0"] = "amusement-parks.fun",
            })
            .Build();
        ServiceCollection services = new ServiceCollection();
        services.AddApiForwardedHeaders(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        ForwardedHeadersOptions options = serviceProvider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;
        ForwardedHeadersMiddleware middleware = new ForwardedHeadersMiddleware(
            static (HttpContext _) => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(options));
        DefaultHttpContext context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:172.30.31.12");
        context.Request.Headers["X-Forwarded-For"] = "192.0.2.123, 203.0.113.42, ::ffff:172.19.0.6";

        await middleware.Invoke(context);

        Assert.Equal(IPAddress.Parse("203.0.113.42"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void AddApiForwardedHeaders_ShouldTrustMappedFormOfConfiguredIpv4Proxy()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownProxies:0"] = "172.30.31.12",
            })
            .Build();
        ServiceCollection services = new ServiceCollection();
        services.AddApiForwardedHeaders(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        ForwardedHeadersOptions options = serviceProvider
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Contains(IPAddress.Parse("::ffff:172.30.31.12"), options.KnownProxies);
    }
}
