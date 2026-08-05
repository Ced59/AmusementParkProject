using System.Net;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.ClientIp;

/// <summary>
/// Resolves the client address previously validated by the forwarded headers middleware.
/// </summary>
public static class ClientIpAddressResolver
{
    public static string? Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        IPAddress? remoteIpAddress = httpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress is null)
        {
            return null;
        }

        IPAddress normalizedAddress = remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4()
            : remoteIpAddress;

        return normalizedAddress.ToString();
    }
}
