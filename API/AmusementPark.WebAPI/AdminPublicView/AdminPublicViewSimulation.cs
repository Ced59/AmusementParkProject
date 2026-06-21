using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace AmusementPark.WebAPI.AdminPublicView;

internal static class AdminPublicViewSimulation
{
    public const string RequestHeaderName = "X-AmusementPark-Public-View-Mode";
    public const string AppliedResponseHeaderName = "X-AmusementPark-Public-View-Mode-Applied";
    public const string HttpContextItemKey = "AdminPublicViewSimulation.Mode";

    public static bool HasRequestHeader(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return headers.ContainsKey(RequestHeaderName);
    }

    public static bool TryReadRequestMode(
        IHeaderDictionary headers,
        out AdminPublicViewSimulationMode mode,
        out string? invalidValue)
    {
        ArgumentNullException.ThrowIfNull(headers);

        mode = default;
        invalidValue = null;

        if (!headers.TryGetValue(RequestHeaderName, out StringValues values))
        {
            return false;
        }

        string rawValue = values.Count == 1
            ? values[0] ?? string.Empty
            : string.Join(",", values.ToArray());
        string value = rawValue.Trim();

        if (TryParseMode(value, out mode))
        {
            return true;
        }

        invalidValue = value;
        return false;
    }

    public static AdminPublicViewSimulationMode? GetAppliedMode(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(HttpContextItemKey, out object? value) && value is AdminPublicViewSimulationMode mode
            ? mode
            : null;
    }

    public static void SetAppliedMode(HttpContext context, AdminPublicViewSimulationMode mode)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[HttpContextItemKey] = mode;
    }

    public static bool CanSeeNonVisible(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        AdminPublicViewSimulationMode? mode = GetAppliedMode(context);
        if (mode.HasValue)
        {
            return mode.Value == AdminPublicViewSimulationMode.AdminPreview
                && context.User?.IsInRole("ADMIN") == true;
        }

        return context.User?.IsInRole("ADMIN") == true;
    }

    public static void ApplyNoStoreHeaders(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.Expires = "0";

        AdminPublicViewSimulationMode? mode = GetAppliedMode(context);
        if (mode.HasValue)
        {
            context.Response.Headers[AppliedResponseHeaderName] = ToHeaderValue(mode.Value);
        }

        AppendVaryHeader(context.Response.Headers, RequestHeaderName);
    }

    public static string ToHeaderValue(AdminPublicViewSimulationMode mode)
    {
        return mode switch
        {
            AdminPublicViewSimulationMode.AnonymousVisitor => "anonymousVisitor",
            AdminPublicViewSimulationMode.UserVisitor => "userVisitor",
            AdminPublicViewSimulationMode.ModeratorVisitor => "moderatorVisitor",
            AdminPublicViewSimulationMode.AdminPreview => "adminPreview",
            _ => string.Empty,
        };
    }

    private static bool TryParseMode(string value, out AdminPublicViewSimulationMode mode)
    {
        mode = value switch
        {
            "anonymousVisitor" => AdminPublicViewSimulationMode.AnonymousVisitor,
            "userVisitor" => AdminPublicViewSimulationMode.UserVisitor,
            "moderatorVisitor" => AdminPublicViewSimulationMode.ModeratorVisitor,
            "adminPreview" => AdminPublicViewSimulationMode.AdminPreview,
            _ => default,
        };

        return value is "anonymousVisitor" or "userVisitor" or "moderatorVisitor" or "adminPreview";
    }

    private static void AppendVaryHeader(IHeaderDictionary headers, string headerName)
    {
        string[] existingValues = headers.Vary.ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string existingValue in existingValues)
        {
            if (string.Equals(existingValue, headerName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        headers.Vary = existingValues.Length == 0
            ? headerName
            : string.Join(", ", existingValues.Concat(new[] { headerName }));
    }
}
