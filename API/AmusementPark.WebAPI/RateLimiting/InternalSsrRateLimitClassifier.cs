using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.RateLimiting;

internal static class InternalSsrRateLimitClassifier
{
    internal const string HeaderName = "X-AmusementPark-Internal-SSR";

    private const string HeaderValue = "1";
    private const string OriginalForHeaderName = "X-Original-For";

    public static bool IsInternalSsrRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues headerValues))
        {
            return false;
        }

        if (!headerValues.Any(value => string.Equals(value, HeaderValue, StringComparison.Ordinal)))
        {
            return false;
        }

        return IsTrustedInternalAddress(context.Connection.RemoteIpAddress)
            || HasTrustedOriginalForAddress(context.Request.Headers);
    }

    private static bool HasTrustedOriginalForAddress(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(OriginalForHeaderName, out StringValues headerValues))
        {
            return false;
        }

        foreach (string? headerValue in headerValues)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            string[] candidateValues = headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string candidateValue in candidateValues)
            {
                if (IPAddress.TryParse(candidateValue, out IPAddress? ipAddress) && IsTrustedInternalAddress(ipAddress))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTrustedInternalAddress(IPAddress? ipAddress)
    {
        if (ipAddress is null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        IPAddress normalizedAddress = ipAddress.IsIPv4MappedToIPv6 ? ipAddress.MapToIPv4() : ipAddress;

        if (normalizedAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = normalizedAddress.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168);
        }

        if (normalizedAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = normalizedAddress.GetAddressBytes();
            return (bytes[0] & 0xfe) == 0xfc;
        }

        return false;
    }
}
