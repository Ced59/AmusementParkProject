using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.OutputCaching;

internal static class SsrInvalidationInputReader
{
    internal static IReadOnlyCollection<string> ResolveStringTargets(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        string routeKey,
        string resultPropertyName,
        string collectionPropertyName)
    {
        HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);

        AddNonEmpty(targets, GetRouteValue(context, routeKey));
        AddNonEmpty(targets, GetStringProperty(ResolveResultValue(executedContext), resultPropertyName));
        AddRange(targets, GetStringCollectionProperty(FindActionArgument(context, "request"), collectionPropertyName));

        return targets.ToList();
    }

    internal static object? ResolveResultValue(ActionExecutedContext? executedContext)
    {
        return executedContext?.Result is ObjectResult objectResult ? objectResult.Value : null;
    }

    internal static object? FindActionArgument(ActionExecutingContext context, string argumentName)
    {
        return context.ActionArguments.TryGetValue(argumentName, out object? value) ? value : null;
    }

    internal static string ResolveControllerName(ActionExecutingContext context)
    {
        return context.ActionDescriptor is ControllerActionDescriptor descriptor
            ? descriptor.ControllerName
            : string.Empty;
    }

    internal static string? GetRouteValue(ActionExecutingContext context, string key)
    {
        object? value = context.RouteData.Values.TryGetValue(key, out object? routeValue) ? routeValue : null;
        return value?.ToString();
    }

    internal static string? GetStringProperty(object? source, string propertyName)
    {
        return GetPropertyValue(source, propertyName) as string;
    }

    internal static string? GetPropertyText(object? source, string propertyName)
    {
        return GetPropertyValue(source, propertyName)?.ToString();
    }

    internal static bool? GetNullableBooleanProperty(object? source, string propertyName)
    {
        return GetPropertyValue(source, propertyName) is bool value ? value : null;
    }

    internal static IReadOnlyCollection<string> GetStringCollectionProperty(object? source, string propertyName)
    {
        return GetPropertyValue(source, propertyName) is IEnumerable<string> values
            ? NormalizeTargets(values).ToList()
            : Array.Empty<string>();
    }

    internal static IEnumerable<string> NormalizeTargets(IEnumerable<string> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal);
    }

    internal static string? NormalizeTarget(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    internal static IEnumerable<string> NormalizePaths(IEnumerable<string> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Select(static value => value.StartsWith("/", StringComparison.Ordinal) ? value : $"/{value}")
            .Distinct(StringComparer.Ordinal);
    }

    internal static void AddNonEmpty(ISet<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value.Trim());
        }
    }

    internal static void AddRange(ISet<string> values, IEnumerable<string> candidates)
    {
        foreach (string candidate in NormalizeTargets(candidates))
        {
            values.Add(candidate);
        }
    }

    internal static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct
    {
        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : null;
    }

    internal static string NormalizeEntityType(string entityType)
    {
        return entityType.Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static object? GetPropertyValue(object? source, string propertyName)
    {
        if (source is null)
        {
            return null;
        }

        PropertyInfo? property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetValue(source);
    }
}
