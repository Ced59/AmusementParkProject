using System.Globalization;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Security;

public sealed class ParkDataEditorOperationCoordinationMiddleware
{
    private const string AnonymousCoordinatedClientId = "anonymous-coordinated-request";
    private readonly RequestDelegate next;

    public ParkDataEditorOperationCoordinationMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IParkDataEditorOperationCoordinator coordinator)
    {
        Endpoint? endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SkipParkDataEditorOperationCoordinationAttribute>() is not null)
        {
            await this.next(context);
            return;
        }

        ParkDataEditorOperationAttribute? operationAttribute =
            endpoint?.Metadata.GetMetadata<ParkDataEditorOperationAttribute>();
        bool isDedicatedToken = IsDedicatedToken(context);
        if (!isDedicatedToken && operationAttribute is null)
        {
            await this.next(context);
            return;
        }

        string clientId = context.User.FindFirst(ParkDataEditorAuthenticationDefaults.TokenIdClaim)?.Value
            ?? AnonymousCoordinatedClientId;
        ParkDataEditorOperationKind kind = operationAttribute?.Kind
            ?? (IsSafeReadMethod(context.Request.Method)
                ? ParkDataEditorOperationKind.Read
                : ParkDataEditorOperationKind.ResourceIntensive);
        string path = context.Request.Path.Value ?? "/";
        ParkDataEditorOperationLease? lease = coordinator.TryBeginRequest(
            clientId,
            kind,
            context.Request.Method,
            path);
        if (lease is null)
        {
            await WriteBusyResponseAsync(context, coordinator.RetryAfterSeconds);
            return;
        }

        using (lease)
        {
            await this.next(context);
        }
    }

    internal static Task WriteBusyResponseAsync(
        HttpContext context,
        int retryAfterSeconds,
        CancellationToken cancellationToken = default)
    {
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            context,
            StatusCodes.Status429TooManyRequests,
            ApiProblemDetailsFactory.GetDefaultTitle(StatusCodes.Status429TooManyRequests),
            "Another park data editor operation is already using the available server capacity. Inspect the global operation status and retry after the indicated delay.",
            "park-data-editor.operation-busy");
        problemDetails.Extensions["retryAfterSeconds"] = retryAfterSeconds;
        problemDetails.Extensions["statusEndpoint"] = "/park-data-editor/operations/status";
        return ApiProblemDetailsFactory.WriteAsync(context, problemDetails, cancellationToken);
    }

    private static bool IsDedicatedToken(HttpContext context)
    {
        string? authenticationMethod = context.User.FindFirst(
            ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim)?.Value;
        return string.Equals(
            authenticationMethod,
            ParkDataEditorAuthenticationDefaults.AuthenticationMethod,
            StringComparison.Ordinal);
    }

    private static bool IsSafeReadMethod(string method)
    {
        return HttpMethods.IsGet(method) || HttpMethods.IsHead(method);
    }
}
