using System.Net;
using AmusementPark.WebAPI.ClientIp;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.ClientIp;

public sealed class ClientIpAddressResolverTests
{
    [Fact]
    public void Resolve_WhenRemoteAddressIsMissing_ShouldReturnNull()
    {
        DefaultHttpContext context = new DefaultHttpContext();

        string? result = ClientIpAddressResolver.Resolve(context);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenRemoteAddressIsIpv4_ShouldReturnCanonicalIpv4Address()
    {
        DefaultHttpContext context = CreateContext("203.0.113.42");

        string? result = ClientIpAddressResolver.Resolve(context);

        Assert.Equal("203.0.113.42", result);
    }

    [Fact]
    public void Resolve_WhenRemoteAddressIsIpv4MappedToIpv6_ShouldReturnCanonicalIpv4Address()
    {
        DefaultHttpContext context = CreateContext("::ffff:203.0.113.42");

        string? result = ClientIpAddressResolver.Resolve(context);

        Assert.Equal("203.0.113.42", result);
    }

    [Fact]
    public void Resolve_WhenRemoteAddressIsIpv6_ShouldPreserveIpv6Address()
    {
        DefaultHttpContext context = CreateContext("2001:db8::42");

        string? result = ClientIpAddressResolver.Resolve(context);

        Assert.Equal("2001:db8::42", result);
    }

    private static DefaultHttpContext CreateContext(string remoteIpAddress)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        return context;
    }
}
