using System;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Diagnostics;

public static class SensitiveRequestPathSanitizer
{
    private const string InvitationPrefix = "/public/trip-invitations/";
    private const string ProxiedInvitationPrefix = "/api/public/trip-invitations/";
    private const string RedactedSegment = "[REDACTED]";

    public static string Sanitize(PathString path)
    {
        string value = path.Value ?? string.Empty;
        string? prefix = ResolvePrefix(value);
        if (prefix is null)
        {
            return value;
        }

        int tokenStart = prefix.Length;
        int suffixStart = value.IndexOf('/', tokenStart);
        if (suffixStart < 0)
        {
            return string.Concat(prefix, RedactedSegment);
        }

        return string.Concat(prefix, RedactedSegment, value[suffixStart..]);
    }

    private static string? ResolvePrefix(string path)
    {
        if (path.StartsWith(InvitationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[..InvitationPrefix.Length];
        }

        if (path.StartsWith(ProxiedInvitationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[..ProxiedInvitationPrefix.Length];
        }

        return null;
    }
}
