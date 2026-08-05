using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.ClientIp;
using AmusementPark.WebAPI.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.Security;

/// <summary>
/// Trace toutes les requêtes authentifiées portant le rôle PARK_DATA_EDITOR,
/// y compris les lectures, les refus et les échecs HTTP.
/// </summary>
public sealed class ParkDataEditorRequestAuditMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ParkDataEditorRequestAuditMiddleware> logger;

    public ParkDataEditorRequestAuditMiddleware(
        RequestDelegate next,
        ILogger<ParkDataEditorRequestAuditMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        int? exceptionStatusCode = null;
        try
        {
            await this.next(context);
        }
        catch
        {
            exceptionStatusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            if (context.User.Identity?.IsAuthenticated == true
                && context.User.IsInRole(AuthorizationRoleGroups.ParkDataEditor))
            {
                IAdminAuditLogWriter writer = context.RequestServices.GetRequiredService<IAdminAuditLogWriter>();
                await this.WriteAuditAsync(context, writer, exceptionStatusCode ?? context.Response.StatusCode);
            }
        }
    }

    private async Task WriteAuditAsync(
        HttpContext context,
        IAdminAuditLogWriter writer,
        int statusCode)
    {
        string? authenticationMethod = context.User.FindFirst(
            ParkDataEditorAuthenticationDefaults.AuthenticationMethodClaim)?.Value;
        string? tokenId = context.User.FindFirst(ParkDataEditorAuthenticationDefaults.TokenIdClaim)?.Value;
        string? tokenLabel = context.User.FindFirst(ParkDataEditorAuthenticationDefaults.TokenLabelClaim)?.Value;
        Dictionary<string, string> metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["authenticationMethod"] = string.IsNullOrWhiteSpace(authenticationMethod) ? "jwt" : authenticationMethod,
        };
        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            metadata["parkDataEditorTokenId"] = tokenId;
        }

        if (!string.IsNullOrWhiteSpace(tokenLabel))
        {
            metadata["parkDataEditorTokenLabel"] = tokenLabel;
        }

        AdminAuditLogEntry entry = new AdminAuditLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Action = "park-data-editor.request",
            EntityType = "HttpRequest",
            EntityId = tokenId,
            ActorUserId = context.User.GetUserId(),
            ActorEmail = context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? context.User.FindFirst(ClaimTypes.Email)?.Value,
            ActorRoles = context.User.FindAll(ClaimTypes.Role)
                .Select(static claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value ?? string.Empty,
            StatusCode = statusCode,
            IpAddress = ClientIpAddressResolver.Resolve(context),
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier,
            Metadata = metadata,
        };

        try
        {
            await writer.WriteAsync(entry, CancellationToken.None);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Park data editor audit write failed for {Method} {Path} with traceId {TraceId}.",
                entry.HttpMethod,
                entry.Path,
                entry.TraceId);
        }
    }
}
