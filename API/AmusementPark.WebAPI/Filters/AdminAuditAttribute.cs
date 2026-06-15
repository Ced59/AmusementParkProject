using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.Filters;

/// <summary>
/// Journalise une action d'administration sensible après exécution réussie.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AdminAuditAttribute : Attribute, IAsyncActionFilter
{
    private static readonly string[] DefaultRouteIdKeys = { "id", "imageId", "userId", "sourceKey", "parkId" };
    private static readonly string[] DefaultPropertyIdKeys = { "Id", "UserId", "IdUser", "ImageId", "SourceKey", "ParkId", "TargetParkId" };
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ReadablePropertiesByType = new ConcurrentDictionary<Type, PropertyInfo[]>();
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> ReadablePropertiesByNameByType = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();
    private static readonly HashSet<string> SensitivePropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newPassword",
        "newPasswordConfirm",
        "token",
        "accessToken",
        "refreshToken",
        "secret",
        "clientSecret",
        "key",
        "file",
        "content",
        "stream",
    };

    public AdminAuditAttribute(string action, string entityType)
    {
        this.Action = action;
        this.EntityType = entityType;
    }

    /// <summary>
    /// Action métier normalisée.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Type d'entité métier concernée.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Nom de la valeur de route contenant l'identifiant cible.
    /// </summary>
    public string? TargetIdRouteKey { get; set; }

    /// <summary>
    /// Identifiant cible statique pour les actions de masse.
    /// </summary>
    public string? StaticTargetId { get; set; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        ActionExecutedContext executedContext = await next();
        if (!this.ShouldAudit(executedContext))
        {
            return;
        }

        IServiceProvider requestServices = context.HttpContext.RequestServices;
        IAdminAuditLogWriter writer = requestServices.GetRequiredService<IAdminAuditLogWriter>();
        ILogger<AdminAuditAttribute> logger = requestServices.GetRequiredService<ILogger<AdminAuditAttribute>>();

        AdminAuditLogEntry entry = this.BuildEntry(context, executedContext);

        try
        {
            await writer.WriteAsync(entry, context.HttpContext.RequestAborted);
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Admin audit log write failed for action {AuditAction} on {EntityType} {EntityId} with traceId {TraceId}.",
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.TraceId);
        }
    }

    private bool ShouldAudit(ActionExecutedContext context)
    {
        if (context.Exception is not null && !context.ExceptionHandled)
        {
            return false;
        }

        int statusCode = GetStatusCode(context.Result);
        return statusCode >= StatusCodes.Status200OK && statusCode < StatusCodes.Status400BadRequest;
    }

    private AdminAuditLogEntry BuildEntry(ActionExecutingContext context, ActionExecutedContext executedContext)
    {
        HttpContext httpContext = context.HttpContext;
        int statusCode = GetStatusCode(executedContext.Result);
        Dictionary<string, string> metadata = this.BuildMetadata(context, executedContext);

        return new AdminAuditLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Action = this.Action,
            EntityType = this.EntityType,
            EntityId = this.ResolveEntityId(context, executedContext),
            ActorUserId = httpContext.User.GetUserId(),
            ActorEmail = httpContext.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value,
            ActorRoles = httpContext.User.FindAll(ClaimTypes.Role).Select(static claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            HttpMethod = httpContext.Request.Method,
            Path = httpContext.Request.Path.Value ?? string.Empty,
            StatusCode = statusCode,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            TraceId = Activity.Current?.Id ?? httpContext.TraceIdentifier,
            Metadata = metadata,
        };
    }

    private string? ResolveEntityId(ActionExecutingContext context, ActionExecutedContext executedContext)
    {
        if (!string.IsNullOrWhiteSpace(this.StaticTargetId))
        {
            return this.StaticTargetId;
        }

        if (!string.IsNullOrWhiteSpace(this.TargetIdRouteKey) && context.RouteData.Values.TryGetValue(this.TargetIdRouteKey, out object? configuredValue))
        {
            return configuredValue?.ToString();
        }

        foreach (string routeIdKey in DefaultRouteIdKeys)
        {
            if (context.RouteData.Values.TryGetValue(routeIdKey, out object? routeValue) && routeValue is not null)
            {
                return routeValue.ToString();
            }
        }

        foreach (object? argumentValue in context.ActionArguments.Values)
        {
            string? argumentId = TryReadAnyScalarProperty(argumentValue, DefaultPropertyIdKeys);
            if (!string.IsNullOrWhiteSpace(argumentId))
            {
                return argumentId;
            }
        }

        if (executedContext.Result is ObjectResult objectResult && objectResult.Value is not null)
        {
            string? resultId = TryReadAnyScalarProperty(objectResult.Value, DefaultPropertyIdKeys);
            if (!string.IsNullOrWhiteSpace(resultId))
            {
                return resultId;
            }
        }

        return null;
    }

    private Dictionary<string, string> BuildMetadata(ActionExecutingContext context, ActionExecutedContext executedContext)
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, object?> routeValue in context.RouteData.Values)
        {
            if (string.Equals(routeValue.Key, "controller", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(routeValue.Key, "action", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddValue(metadata, $"route.{routeValue.Key}", routeValue.Value?.ToString());
        }

        foreach (KeyValuePair<string, object?> actionArgument in context.ActionArguments)
        {
            this.AddArgumentMetadata(metadata, actionArgument.Key, actionArgument.Value);
        }

        AddValue(metadata, "result.statusCode", GetStatusCode(executedContext.Result).ToString());
        return metadata;
    }

    private void AddArgumentMetadata(Dictionary<string, string> metadata, string argumentName, object? argumentValue)
    {
        if (argumentValue is null || IsSensitiveName(argumentName))
        {
            return;
        }

        Type valueType = argumentValue.GetType();
        if (TryFormatScalar(argumentValue, out string? scalarValue))
        {
            AddValue(metadata, $"argument.{argumentName}", scalarValue);
            return;
        }

        if (argumentValue is IEnumerable enumerable && argumentValue is not string)
        {
            AddValue(metadata, $"argument.{argumentName}.count", CountEnumerable(enumerable).ToString());
            return;
        }

        foreach (PropertyInfo propertyInfo in GetReadableProperties(valueType))
        {
            if (IsSensitiveName(propertyInfo.Name))
            {
                continue;
            }

            object? propertyValue = propertyInfo.GetValue(argumentValue);
            if (propertyValue is null)
            {
                continue;
            }

            if (TryFormatScalar(propertyValue, out string? propertyScalarValue))
            {
                AddValue(metadata, $"argument.{argumentName}.{propertyInfo.Name}", propertyScalarValue);
                continue;
            }

            if (propertyValue is IEnumerable propertyEnumerable && propertyValue is not string)
            {
                AddValue(metadata, $"argument.{argumentName}.{propertyInfo.Name}.count", CountEnumerable(propertyEnumerable).ToString());
            }
        }
    }

    private static int GetStatusCode(IActionResult? actionResult)
    {
        return actionResult switch
        {
            ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            JsonResult jsonResult => jsonResult.StatusCode ?? StatusCodes.Status200OK,
            ContentResult contentResult => contentResult.StatusCode ?? StatusCodes.Status200OK,
            EmptyResult => StatusCodes.Status204NoContent,
            FileResult => StatusCodes.Status200OK,
            _ => StatusCodes.Status200OK,
        };
    }

    private static bool TryFormatScalar(object value, out string? scalarValue)
    {
        scalarValue = value switch
        {
            string text => Truncate(text, 500),
            int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            bool boolean => boolean.ToString(),
            Guid guid => guid.ToString("D"),
            DateTime dateTime => dateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            Enum enumValue => enumValue.ToString(),
            StringValues stringValues => Truncate(stringValues.ToString(), 500),
            _ => null,
        };

        return scalarValue is not null;
    }

    private static string? TryReadAnyScalarProperty(object? value, IReadOnlyCollection<string> propertyNames)
    {
        if (value is null)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            string? propertyValue = TryReadScalarProperty(value, propertyName);
            if (!string.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }
        }

        return null;
    }

    private static string? TryReadScalarProperty(object value, string propertyName)
    {
        IReadOnlyDictionary<string, PropertyInfo> readablePropertiesByName = ReadablePropertiesByNameByType.GetOrAdd(value.GetType(), BuildReadablePropertiesByName);
        if (!readablePropertiesByName.TryGetValue(propertyName, out PropertyInfo? propertyInfo))
        {
            return null;
        }

        object? propertyValue = propertyInfo.GetValue(value);
        return propertyValue is not null && TryFormatScalar(propertyValue, out string? scalarValue) ? scalarValue : null;
    }

    private static bool IsSensitiveName(string name)
    {
        foreach (string sensitiveName in SensitivePropertyNames)
        {
            if (name.Contains(sensitiveName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddValue(Dictionary<string, string> metadata, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        metadata[key] = Truncate(value, 500);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }

    private static PropertyInfo[] GetReadableProperties(Type type)
    {
        return ReadablePropertiesByType.GetOrAdd(type, static valueType => valueType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static propertyInfo => propertyInfo.CanRead && propertyInfo.GetIndexParameters().Length == 0)
            .ToArray());
    }

    private static IReadOnlyDictionary<string, PropertyInfo> BuildReadablePropertiesByName(Type type)
    {
        Dictionary<string, PropertyInfo> propertiesByName = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (PropertyInfo propertyInfo in GetReadableProperties(type))
        {
            propertiesByName[propertyInfo.Name] = propertyInfo;
        }

        return propertiesByName;
    }

    private static int CountEnumerable(IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
        {
            return collection.Count;
        }

        int count = 0;
        IEnumerator enumerator = enumerable.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                count++;
            }
        }
        finally
        {
            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return count;
    }
}
