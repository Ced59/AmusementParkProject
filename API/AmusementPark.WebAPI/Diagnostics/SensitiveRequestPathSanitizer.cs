using System;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Diagnostics;

public static class SensitiveRequestPathSanitizer
{
    private const string FrontInvitationSegment = "/trip-invitations/";
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

    public static string? SanitizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? absoluteUri)
            && (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            string sanitizedPath = SanitizeUrlPath(absoluteUri.AbsolutePath);
            if (string.Equals(sanitizedPath, absoluteUri.AbsolutePath, StringComparison.Ordinal))
            {
                return value;
            }

            return string.Concat(absoluteUri.GetLeftPart(UriPartial.Authority), sanitizedPath);
        }

        if (!value.StartsWith('/', StringComparison.Ordinal))
        {
            return value;
        }

        return SanitizeUrlPath(value);
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

    private static string SanitizeUrlPath(string path)
    {
        string sanitizedApiPath = Sanitize(new PathString(path));
        if (!string.Equals(sanitizedApiPath, path, StringComparison.Ordinal))
        {
            return sanitizedApiPath;
        }

        int invitationSegmentStart = path.IndexOf(FrontInvitationSegment, StringComparison.OrdinalIgnoreCase);
        if (invitationSegmentStart < 0)
        {
            return path;
        }

        int tokenStart = invitationSegmentStart + FrontInvitationSegment.Length;
        if (tokenStart >= path.Length)
        {
            return path;
        }

        int suffixStart = path.IndexOfAny(['/', '?', '#'], tokenStart);
        if (suffixStart < 0)
        {
            return string.Concat(path.AsSpan(0, tokenStart), RedactedSegment);
        }

        return string.Concat(path.AsSpan(0, tokenStart), RedactedSegment, path.AsSpan(suffixStart));
    }
}
